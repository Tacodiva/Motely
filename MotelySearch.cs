
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Motely;


public ref struct MotelyFilterCreationContext
{

    private readonly HashSet<int> _cachedPseudohashKeyLengths;
    public readonly IReadOnlyCollection<int> CachedPseudohashKeyLengths => _cachedPseudohashKeyLengths;

    public MotelyFilterCreationContext()
    {
        _cachedPseudohashKeyLengths = [0];
    }

    public readonly void RemoveCachedPseudoHash(int keyLength)
    {
        _cachedPseudohashKeyLengths.Remove(keyLength);
    }

    public readonly void RemoveCachedPseudoHash(string key)
    {
        RemoveCachedPseudoHash(key.Length);
    }

    public readonly void CachePseudoHash(int keyLength)
    {
        _cachedPseudohashKeyLengths.Add(keyLength);
    }

    public readonly void CachePseudoHash(string key)
    {
        CachePseudoHash(key.Length);
    }

    private readonly void CacheResampleStream(string key)
    {
        CachePseudoHash(key);
        CachePseudoHash(key + MotelyPrngKeys.Resample + "X");
        // We don't cache resamples > 8 because they'd use an extra digit
    }

    public readonly void CacheBoosterPackStream(int ante) => CachePseudoHash(MotelyPrngKeys.ShopPack + ante);

    public readonly void CacheTagStream(int ante) => CachePseudoHash(MotelyPrngKeys.Tags + ante);

    public readonly void CacheVoucherStream(int ante) => CacheResampleStream(MotelyPrngKeys.Voucher + ante);

    public readonly void CacheTarotStream(int ante)
    {
        CacheResampleStream(MotelyPrngKeys.Tarot + MotelyPrngKeys.ArcanaPack + ante);
        CachePseudoHash(MotelyPrngKeys.TerrotSoul + MotelyPrngKeys.Tarot + ante);
    }

}

public interface IMotelySeedFilterDesc<TFilter> where TFilter : struct, IMotelySeedFilter
{
    public TFilter CreateFilter(ref MotelyFilterCreationContext ctx);
}

public interface IMotelySeedFilter
{
    public VectorMask Filter(ref MotelyVectorSearchContext searchContext);
}

public sealed class MotelySearchSettings<TFilter>(IMotelySeedFilterDesc<TFilter> filterDesc)
    where TFilter : struct, IMotelySeedFilter
{
    public int ThreadCount { get; set; } = Environment.ProcessorCount;

    public IMotelySeedFilterDesc<TFilter> FilterDesc { get; set; } = filterDesc;

    public MotelySearchSettings<TFilter> WithThreadCount(int threadCount)
    {
        ThreadCount = threadCount;
        return this;
    }

    public Task Start() => Task.Run(() =>
    {
        new MotelySearch<TFilter>(this).Search();
    });
}

#if !DEBUG
[method:MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
internal unsafe struct SeedHashCache(Vector512<double>* seedHashes, int* seedHashesLookup)
{
    // A map of pseudohash key length => cache index
    public readonly int* SeedHashesLookup = seedHashesLookup;

    // A list of all the cached seed hashs
    public readonly Vector512<double>* SeedHashes = seedHashes;

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly bool HasPartialHash(int keyLength)
    {
        return keyLength < Motely.MaxCachedPseudoHashKeyLength && SeedHashesLookup[keyLength] != -1;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly Vector512<double> GetSeedHashVector()
    {
        Debug.Assert(SeedHashesLookup[0] == 0);
        return SeedHashes[0];
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly double GetSeedHash(int lane)
    {
        return GetSeedHashVector()[lane];
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly Vector512<double> GetPartialHashVector(int keyLength)
    {
        return SeedHashes[SeedHashesLookup[keyLength]];
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly double GetPartialHash(int keyLength, int lane)
    {
        return GetPartialHashVector(keyLength)[lane];
    }
}

public unsafe sealed class MotelySearch<TFilter>
    where TFilter : struct, IMotelySeedFilter
{
    // A cache of vectors containing all the seed's digits.
    private static readonly Vector512<double>[] SeedDigitVectors = new Vector512<double>[(Motely.SeedDigits.Length + Vector512<double>.Count - 1) / Vector512<double>.Count];

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

    private const int NonBatchedCharacters = 4;
    private readonly static int MaxBatch = (int)Math.Pow(Motely.SeedDigits.Length, NonBatchedCharacters);
    private readonly static int SeedsPerBatch = (int)Math.Pow(Motely.SeedDigits.Length, Motely.MaxSeedLength - NonBatchedCharacters);

    private readonly int _threadCount;
    private readonly TFilter _filter;
    private readonly int _pseudoHashKeyLengthCount;
    private readonly int* _pseudoHashKeyLengths;
    private readonly int* _pseudoHashReverseMap;

    private int _batchIndex;
    private int _finishedBatchCount;
    private readonly Stopwatch _elapsedTime = new();

    public MotelySearch(MotelySearchSettings<TFilter> settings)
    {
        _threadCount = settings.ThreadCount;

        MotelyFilterCreationContext filterCreationContext = new();
        _filter = settings.FilterDesc.CreateFilter(ref filterCreationContext);

        int[] pseudohashKeyLengths = filterCreationContext.CachedPseudohashKeyLengths.ToArray();
        _pseudoHashKeyLengthCount = pseudohashKeyLengths.Length;
        _pseudoHashKeyLengths = (int*)Marshal.AllocHGlobal(sizeof(int) * _pseudoHashKeyLengthCount);

        for (int i = 0; i < _pseudoHashKeyLengthCount; i++)
        {
            _pseudoHashKeyLengths[i] = pseudohashKeyLengths[i];
        }

        _pseudoHashReverseMap = (int*)Marshal.AllocHGlobal(sizeof(int) * Motely.MaxCachedPseudoHashKeyLength);

        for (int i = 0; i < Motely.MaxCachedPseudoHashKeyLength; i++)
            _pseudoHashReverseMap[i] = -1;

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

        _batchIndex = -1;
        // _batchIndex = 0;

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
        public readonly MotelySearch<TFilter> Search;

        private readonly char* _digits;

        public MotelySearchThread(MotelySearch<TFilter> search, int index)
        {
            ThreadIndex = index;
            Search = search;

            Thread = new(ThreadMain)
            {
                Name = $"Motely Search Thread {index}"
            };

            _digits = (char*)Marshal.AllocHGlobal(sizeof(char) * Motely.MaxSeedLength);
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

                string timeLeftFormatted;

                if (timeLeftSpan.Days == 0) timeLeftFormatted = $"{timeLeftSpan:hh\\:mm\\:ss}";
                else timeLeftFormatted =  $"{timeLeftSpan:d\\:hh\\:mm\\:ss}";

                FancyConsole.SetBottomLine($"{Math.Round(portionFinished * 100, 2):F2}% ~{timeLeftFormatted} remaining ({Math.Round(seedsPerMS)} seeds/ms)");
            }
        }

        private void SearchBatch(int batchIdx)
        {

            // Figure out which digits this search is doing
            for (int i = 0; i < NonBatchedCharacters; i++)
            {
                int charIndex = batchIdx % Motely.SeedDigits.Length;
                _digits[Motely.MaxSeedLength - i - 1] = Motely.SeedDigits[charIndex];
                batchIdx /= Motely.SeedDigits.Length;
            }

            Vector512<double>* hashes = stackalloc Vector512<double>[Search._pseudoHashKeyLengthCount];

            // Calculate hash for the first two digits at all the required pseudohash lengths
            for (int pseudohashKeyIdx = 0; pseudohashKeyIdx < Search._pseudoHashKeyLengthCount; pseudohashKeyIdx++)
            {
                int pseudohashKeyLength = Search._pseudoHashKeyLengths[pseudohashKeyIdx];

                double num = 1;

                for (int i = Motely.MaxSeedLength - 1; i > Motely.MaxSeedLength - 1 - NonBatchedCharacters; i--)
                {
                    num = (1.1239285023 / num * _digits[i] * Math.PI + (i + pseudohashKeyLength + 1) * Math.PI) % 1;
                }

                hashes[pseudohashKeyIdx] = Vector512.Create(num);
            }


            // Start searching
            for (int vectorIndex = 0; vectorIndex < SeedDigitVectors.Length; vectorIndex++)
            {
                SearchVector(Motely.MaxSeedLength - 1 - NonBatchedCharacters, SeedDigitVectors[vectorIndex], hashes, 0);
            }
        }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void SearchVector(int i, Vector512<double> seedDigitVector, Vector512<double>* nums, int numsLaneIndex)
        {
            Vector512<double>* hashes = stackalloc Vector512<double>[Search._pseudoHashKeyLengthCount];

            for (int pseudohashKeyIdx = 0; pseudohashKeyIdx < Search._pseudoHashKeyLengthCount; pseudohashKeyIdx++)
            {
                int pseudohashKeyLength = Search._pseudoHashKeyLengths[pseudohashKeyIdx];

                Vector512<double> calcVector = Vector512.Divide(Vector512.Create(1.1239285023), ((double*)&nums[pseudohashKeyIdx])[numsLaneIndex]);

                calcVector = Vector512.Multiply(calcVector, seedDigitVector);

                calcVector = Vector512.Multiply(calcVector, Math.PI);
                calcVector = Vector512.Add(calcVector, Vector512.Create((i + pseudohashKeyLength + 1) * Math.PI));

                Vector512<double> intPart = Vector512.Floor(calcVector);
                calcVector = Vector512.Subtract(calcVector, intPart);

                hashes[pseudohashKeyIdx] = calcVector;
            }

            if (i == 0)
            {
                MotelySearchContextParams searchContextParams = new(
                    new(hashes, Search._pseudoHashReverseMap),
                    Motely.MaxSeedLength, &_digits[1], seedDigitVector
                );

                MotelyVectorSearchContext searchContext = new(ref searchContextParams);
                uint successMask = Search._filter.Filter(ref searchContext).Value;

                if (successMask != 0)
                {
                    for (int lane = 0; lane < Vector512<double>.Count; lane++)
                    {
                        if ((successMask & 1) != 0)
                        {
                            _digits[0] = (char)seedDigitVector[lane];

                            string seed = "";

                            for (int digit = 0; digit < Motely.MaxSeedLength; digit++)
                            {
                                if (_digits[digit] != '\0')
                                    seed += _digits[digit];
                            }

                            FancyConsole.WriteLine($"{seed}");
                        }

                        successMask >>= 1;
                    }
                }

                // Environment.Exit(0);
            }
            else
            {
                for (int lane = 0; lane < Vector512<double>.Count; lane++)
                {
                    if (seedDigitVector[lane] == 0) break;

                    _digits[i] = (char)seedDigitVector[lane];

                    for (int vectorIndex = 0; vectorIndex < SeedDigitVectors.Length; vectorIndex++)
                    {
                        SearchVector(i - 1, SeedDigitVectors[vectorIndex], hashes, lane);
                    }
                }
            }
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal((nint)_digits);
        }
    }
}