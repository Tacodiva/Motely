
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Motely;


public ref struct MotelyFilterCreationContext()
{

    private readonly HashSet<int> _pseudohashKeyLengths = new();
    public IReadOnlyCollection<int> PseudohashKeyLengths => _pseudohashKeyLengths;

    public readonly void RegisterPseudoHash(string key)
    {
        _pseudohashKeyLengths.Add(key.Length);
    }

    public readonly void RegisterPseudoRNG(string key)
    {
        _pseudohashKeyLengths.Add(0);
        RegisterPseudoHash(key);
    }

}

public interface IMotelySeedFilterDesc<TFilter> where TFilter : struct, IMotelySeedFilter
{
    public TFilter CreateFilter(ref MotelyFilterCreationContext ctx);
}

public interface IMotelySeedFilter
{
    public Vector512<double> Filter(ref MotelySearchContext searchContext);
}


public struct MotelyPrngStream(Vector512<double> state)
{
    public Vector512<double> State = state;
}

public sealed class MotelySearchSettings<TFilterDesc, TFilter>
    where TFilterDesc : struct, IMotelySeedFilterDesc<TFilter>
    where TFilter : struct, IMotelySeedFilter
{

    public int ThreadCount { get; set; } = Environment.ProcessorCount;

    public TFilterDesc FilterDesc;

    public MotelySearchSettings(TFilterDesc filterDesc)
    {
        FilterDesc = filterDesc;
    }

    public MotelySearchSettings<TFilterDesc, TFilter> WithThreadCount(int threadCount)
    {
        ThreadCount = threadCount;
        return this;
    }

    public void Search()
    {
        new MotelySearch<TFilterDesc, TFilter>(this).Search();
    }
}

public unsafe ref struct MotelySearchContext(Vector512<double>* seedHashes, int* seedHashesLookup)
{

    private readonly int* _seedHashesLookup = seedHashesLookup;
    // A map of pseudohash key length => seed hash up to that point
    private readonly Vector512<double>* _seedHashes = seedHashes;

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> PseudoHash(string key)
    {
#if MOTELY_SAFE
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(key.Length, Motely.MaxPseudoHashKeyLength);
#endif
        Vector512<double> calcVector = _seedHashes[_seedHashesLookup[key.Length]];

        for (int i = key.Length - 1; i >= 0; i--)
        {
            calcVector = Vector512.Divide(Vector512.Create(1.1239285023), calcVector);

            calcVector = Vector512.Multiply(calcVector, key[i]);

            calcVector = Vector512.Multiply(calcVector, Math.PI);
            calcVector = Vector512.Add(calcVector, Vector512.Create((i + 1) * Math.PI));

            Vector512<double> intPart = Vector512.Floor(calcVector);
            calcVector = Vector512.Subtract(calcVector, intPart);
        }

        return calcVector;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePRNG(Vector512<double> state)
    {
        state = Vector512.Multiply(state, 1.72431234);
        state = Vector512.Add(state, Vector512.Create(2.134453429141));

        Vector512<double> intPart = Vector512.Floor(state);
        state = Vector512.Subtract(state, intPart);

        state = Vector512.Multiply(state, 10000000000000);

        state = Vector512.Round(state);
        state = Vector512.Divide(state, 10000000000000);

        return state;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyPrngStream GetPrngStream(string key)
    {
        return new(PseudoHash(key));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public void IteratePrngStream(ref MotelyPrngStream stream)
    {
        stream.State = IteratePRNG(stream.State);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePseudoSeed(ref MotelyPrngStream stream)
    {
        IteratePrngStream(ref stream);
        return (stream.State + _seedHashes[0]) / Vector512.Create<double>(2);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePrngRandom(ref MotelyPrngStream stream)
    {
        return VectorLuaRandomSingle.Random(IteratePseudoSeed(ref stream));
    }


}

public unsafe sealed class MotelySearch<TFilterDesc, TFilter>
    where TFilterDesc : struct, IMotelySeedFilterDesc<TFilter>
    where TFilter : struct, IMotelySeedFilter
{
    // A cache of vectors containing all the seed's digits.
    private static Vector512<double>[] SeedDigitVectors = new Vector512<double>[(Motely.SeedDigits.Length + Vector512<double>.Count - 1) / Vector512<double>.Count];

    [SkipLocalsInit]
    static MotelySearch()
    {
        Span<double> vector = stackalloc double[Vector512<double>.Count];

        for (int i = 0; i < SeedDigitVectors.Length; i++)
        {
            for (int j = 0; j < Vector512<double>.Count; j++)
            {
                int index = i * Vector512<double>.Count + j;

                if (index >= Motely.SeedDigits.Length)
                {
                    vector[j] = 0;
                }
                else
                {
                    vector[j] = Motely.SeedDigits[index];
                }
            }

            SeedDigitVectors[i] = Vector512.Create<double>(vector);
        }
    }

    private const int BatchCharacters = 3;
    private readonly static int MaxBatch = (int)Math.Pow(Motely.SeedDigits.Length, BatchCharacters);
    private readonly static int SeedsPerBatch = (int)Math.Pow(Motely.SeedDigits.Length, Motely.MaxSeedLength - BatchCharacters);

    private readonly int _threadCount;
    private readonly TFilter _filter;
    private readonly int _pseudoHashKeyLengthCount;
    private readonly int* _pseudoHashKeyLengths;
    private readonly int* _pseudoHashReverseMap;

    private int _batchIndex;
    private int _finishedBatchCount;
    private readonly Stopwatch _elapsedTime = new();

    public MotelySearch(MotelySearchSettings<TFilterDesc, TFilter> settings)
    {
        _threadCount = settings.ThreadCount;

        MotelyFilterCreationContext filterCreationContext = new();
        _filter = settings.FilterDesc.CreateFilter(ref filterCreationContext);

        int[] pseudohashKeyLengths = filterCreationContext.PseudohashKeyLengths.ToArray();
        _pseudoHashKeyLengthCount = pseudohashKeyLengths.Length;
        _pseudoHashKeyLengths = (int*)Marshal.AllocHGlobal(sizeof(int) * _pseudoHashKeyLengthCount);

        for (int i = 0; i < _pseudoHashKeyLengthCount; i++)
        {
            _pseudoHashKeyLengths[i] = pseudohashKeyLengths[i];
        }

        _pseudoHashReverseMap = (int*)Marshal.AllocHGlobal(sizeof(int) * Motely.MaxPseudoHashKeyLength);

        for (int i = 0; i < _pseudoHashKeyLengthCount; i++)
        {
            _pseudoHashReverseMap[_pseudoHashKeyLengths[i]] = i;
        }
    }

    public void Search()
    {
        _elapsedTime.Restart();
        FancyConsole.WriteLine("Starting search...");

        MotelySearchThread[] threads = new MotelySearchThread[_threadCount];

        for (int i = 0; i < _threadCount; i++)
        {
            threads[i] = new(this, i);
        }

        // _batchIndex = -2;
        _batchIndex = -1;

        foreach (MotelySearchThread thread in threads)
            thread.Thread.Start();

        foreach (MotelySearchThread thread in threads)
            thread.Thread.Join();

        _elapsedTime.Stop();

        FancyConsole.SetBottomLine(null);

        Console.WriteLine($"Search took {_elapsedTime.ElapsedMilliseconds}ms");
    }

    public sealed unsafe class MotelySearchThread : IDisposable
    {
        public readonly int ThreadIndex;
        public readonly Thread Thread;
        public readonly MotelySearch<TFilterDesc, TFilter> Search;

        private char* digits;

        public MotelySearchThread(MotelySearch<TFilterDesc, TFilter> search, int index)
        {
            ThreadIndex = index;
            Search = search;

            Thread = new(ThreadMain)
            {
                Name = $"Motely Search Thread {index}"
            };

            digits = (char*)Marshal.AllocHGlobal(sizeof(char) * Motely.MaxSeedLength);
        }

        private void ThreadMain()
        {
            while (true)
            {
                int batchIdx = Interlocked.Increment(ref Search._batchIndex);

                if (batchIdx >= MaxBatch)
                {
                    break;
                }

                if (batchIdx == -1)
                {
                    // TODO Search all lengths less than 1
                }
                else
                {
                    SearchBatch(batchIdx);
                }

                int finishedCount = Interlocked.Increment(ref Search._finishedBatchCount);

                double portionFinished = finishedCount / (double)MaxBatch;
                double elapsedMS = Search._elapsedTime.ElapsedMilliseconds;
                double totalTimeEstimate = elapsedMS / portionFinished;
                double timeLeft = totalTimeEstimate - elapsedMS;

                TimeSpan timeLeftSpan = TimeSpan.FromMilliseconds(timeLeft);

                double seedsPerMS = (finishedCount * (double)SeedsPerBatch) / elapsedMS;

                FancyConsole.SetBottomLine($"{Math.Round(portionFinished * 100, 2):F2}% ~{timeLeftSpan:hh\\:mm\\:ss} remaining ({Math.Round(seedsPerMS)} seeds/ms)");
            }
        }

        [SkipLocalsInit]
        private void SearchBatch(int batchIdx)
        {

            // Figure out which digits this search is doing
            for (int i = 0; i < BatchCharacters; i++)
            {
                int charIndex = batchIdx % Motely.SeedDigits.Length;
                digits[Motely.MaxSeedLength - i - 1] = Motely.SeedDigits[charIndex];
                batchIdx /= Motely.SeedDigits.Length;
            }

            Vector512<double>* hashes = stackalloc Vector512<double>[Search._pseudoHashKeyLengthCount];

            // Calculate hash for the first two digits at all the required pseudohash lengths
            for (int pseudohashKeyIdx = 0; pseudohashKeyIdx < Search._pseudoHashKeyLengthCount; pseudohashKeyIdx++)
            {
                int pseudohashKeyLength = Search._pseudoHashKeyLengths[pseudohashKeyIdx];

                double num = 1;

                for (int i = Motely.MaxSeedLength - 1; i > Motely.MaxSeedLength - 1 - BatchCharacters; i--)
                {
                    num = (1.1239285023 / num * digits[i] * Math.PI + (i + pseudohashKeyLength + 1) * Math.PI) % 1;
                }

                hashes[pseudohashKeyIdx] = Vector512.Create(num);
            }


            // Start searching
            for (int vectorIndex = 0; vectorIndex < SeedDigitVectors.Length; vectorIndex++)
            {
                SearchVector(Motely.MaxSeedLength - 1 - BatchCharacters, SeedDigitVectors[vectorIndex], hashes, 0);
            }
        }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        [SkipLocalsInit]
        private void SearchVector(int i, Vector512<double> seedDigitVector, Vector512<double>* nums, int numsChannel)
        {
            Vector512<double>* hashes = stackalloc Vector512<double>[Search._pseudoHashKeyLengthCount];

            for (int pseudohashKeyIdx = 0; pseudohashKeyIdx < Search._pseudoHashKeyLengthCount; pseudohashKeyIdx++)
            {
                int pseudohashKeyLength = Search._pseudoHashKeyLengths[pseudohashKeyIdx];



                Vector512<double> calcVector = Vector512.Divide(Vector512.Create(1.1239285023), ((double*)&nums[pseudohashKeyIdx])[numsChannel]);

                calcVector = Vector512.Multiply(calcVector, seedDigitVector);

                calcVector = Vector512.Multiply(calcVector, Math.PI);
                calcVector = Vector512.Add(calcVector, Vector512.Create((i + pseudohashKeyLength + 1) * Math.PI));

                Vector512<double> intPart = Vector512.Floor(calcVector);
                calcVector = Vector512.Subtract(calcVector, intPart);

                hashes[pseudohashKeyIdx] = calcVector;
            }

            if (i == 0)
            {
                MotelySearchContext searchContext = new(hashes, Search._pseudoHashReverseMap);
                Vector512<double> results = Search._filter.Filter(ref searchContext);

                if (!Vector512.EqualsAll(results, Vector512<double>.Zero))
                {
                    for (int channel = 0; channel < Vector512<double>.Count; channel++)
                    {
                        if (results[channel] != 0)
                        {
                            digits[0] = (char)seedDigitVector[channel];

                            string seed = "";

                            for (int digit = 0; digit < Motely.MaxSeedLength; digit++)
                            {
                                if (digits[digit] != '\0')
                                    seed += digits[digit];
                            }

                            FancyConsole.WriteLine($"{seed}");
                        }
                    }
                }
            }
            else
            {
                for (int channel = 0; channel < Vector512<double>.Count; channel++)
                {
                    if (seedDigitVector[channel] == 0) break;

                    digits[i] = (char)seedDigitVector[channel];

                    for (int vectorIndex = 0; vectorIndex < SeedDigitVectors.Length; vectorIndex++)
                    {
                        SearchVector(i - 1, SeedDigitVectors[vectorIndex], hashes, channel);
                    }
                }
            }
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal((nint)digits);
        }
    }
}