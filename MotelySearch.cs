using System.ComponentModel;
using System.Diagnostics;
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

public enum MotelySearchMode
{
    Sequential,
    Provider
}

public interface IMotelySeedProvider
{
    public int SeedCount { get; }
    public ReadOnlySpan<char> NextSeed();
}

public sealed class MotelyRandomSeedProvider(int count) : IMotelySeedProvider
{
    public int SeedCount { get; } = count;

    private readonly ThreadLocal<Random> _randomInstances = new();

    public ReadOnlySpan<char> NextSeed()
    {
        Random? random = _randomInstances.Value ??= new();

        Span<char> seed = stackalloc char[Motely.MaxSeedLength];

        for (int i = 0; i < seed.Length; i++)
        {
            seed[i] = Motely.SeedDigits[random.Next(Motely.SeedDigits.Length)];
        }

        return new string(seed);
    }
}

public sealed class MotelySeedListProvider(IEnumerable<string> seeds) : IMotelySeedProvider
{
    // Sort the seeds by length to increase vectorization potential
    public readonly string[] Seeds = [.. seeds.OrderBy(seed => seed.Length)];

    public int SeedCount => Seeds.Length;

    private int _currentSeed = -1;
    public ReadOnlySpan<char> NextSeed() => Seeds[Interlocked.Increment(ref _currentSeed)];
}

public sealed class MotelySearchSettings<TFilter>(IMotelySeedFilterDesc<TFilter> filterDesc)
    where TFilter : struct, IMotelySeedFilter
{
    public int ThreadCount { get; set; } = Environment.ProcessorCount;
    public int StartBatchIndex { get; set; } = 0;


    public IMotelySeedFilterDesc<TFilter> FilterDesc { get; set; } = filterDesc;

    public MotelySearchMode Mode;

    /// <summary>
    /// The object which provides seeds to search. Should only be non-null if
    /// `Mode` is set to `Provider`.
    /// </summary>
    public IMotelySeedProvider? SeedProvider;

    /// <summary>
    /// The number of seed characters each batch contains.
    ///  
    /// For example, with a value of 3 one batch would go through 35^3 seeds.
    /// Only meaningful when `Mode` is set to `Sequential`.
    /// </summary>
    public int SequentialBatchCharacterCount { get; set; } = 3;

    public MotelySearchSettings<TFilter> WithThreadCount(int threadCount)
    {
        ThreadCount = threadCount;
        return this;
    }

    public MotelySearchSettings<TFilter> WithStartBatchIndex(int startBatchIndex)
    {
        StartBatchIndex = startBatchIndex;
        return this;
    }

    public MotelySearchSettings<TFilter> WithBatchCharacterCount(int batchCharacterCount)
    {
        SequentialBatchCharacterCount = batchCharacterCount;
        return this;
    }

    public MotelySearchSettings<TFilter> WithListSearch(IEnumerable<string> seeds)
    {
        return WithProviderSearch(new MotelySeedListProvider(seeds));
    }

    public MotelySearchSettings<TFilter> WithProviderSearch(IMotelySeedProvider provider)
    {
        SeedProvider = provider;
        Mode = MotelySearchMode.Provider;
        return this;
    }

    public MotelySearchSettings<TFilter> WithSequentialSearch()
    {
        SeedProvider = null;
        Mode = MotelySearchMode.Sequential;
        return this;
    }

    public IMotelySearch Start()
    {
        MotelySearch<TFilter> search = new(this);

        search.Start();

        return search;
    }
}

public interface IMotelySearch : IDisposable
{
    public MotelySearchStatus Status { get; }
    public int BatchIndex { get; }
    public int CompletedBatchCount { get; }

    public void Start();
    public void Pause();
}

public enum MotelySearchStatus
{
    Paused,
    Running,
    Completed,
    Disposed
}

public unsafe sealed class MotelySearch<TFilter> : IMotelySearch
    where TFilter : struct, IMotelySeedFilter
{
    private readonly MotelySearchThread[] _threads;
    private readonly Barrier _pauseBarrier;
    private readonly Barrier _unpauseBarrier;
    private volatile MotelySearchStatus _status;
    public MotelySearchStatus Status => _status;

    private readonly TFilter _filter;
    private readonly int _pseudoHashKeyLengthCount;
    private readonly int* _pseudoHashKeyLengths;
    private readonly int* _pseudoHashReverseMap;

    private readonly int _startBatchIndex;
    private int _batchIndex;
    public int BatchIndex => _batchIndex;
    private int _completedBatchCount;
    public int CompletedBatchCount => _completedBatchCount;

    private double _lastReportMS;

    private readonly Stopwatch _elapsedTime = new();

    public MotelySearch(MotelySearchSettings<TFilter> settings)
    {
        MotelyFilterCreationContext filterCreationContext = new();
        _filter = settings.FilterDesc.CreateFilter(ref filterCreationContext);

        _startBatchIndex = settings.StartBatchIndex;
        _batchIndex = _startBatchIndex;
        _completedBatchCount = _startBatchIndex;

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

        _pauseBarrier = new(settings.ThreadCount + 1);
        _unpauseBarrier = new(settings.ThreadCount + 1);
        _status = MotelySearchStatus.Paused;

        _threads = new MotelySearchThread[settings.ThreadCount];
        for (int i = 0; i < _threads.Length; i++)
        {
            _threads[i] = settings.Mode switch
            {
                MotelySearchMode.Sequential => new MotelySequentialSearchThread(this, settings, i),
                MotelySearchMode.Provider => new MotelyProviderSearchThread(this, settings, i),
                _ => throw new InvalidEnumArgumentException(nameof(settings.Mode))
            };
        }

        // The threads all immediatly enter a paused state
        _pauseBarrier.SignalAndWait();
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_status == MotelySearchStatus.Disposed, this);
        // Atomically replace paused status with running
        if (Interlocked.CompareExchange(ref _status, MotelySearchStatus.Running, MotelySearchStatus.Paused) != MotelySearchStatus.Paused)
            return;

        _elapsedTime.Start();
        _unpauseBarrier.SignalAndWait();
    }

    public void Pause()
    {
        ObjectDisposedException.ThrowIf(_status == MotelySearchStatus.Disposed, this);
        // Atomically replace running status with paused
        if (Interlocked.CompareExchange(ref _status, MotelySearchStatus.Paused, MotelySearchStatus.Running) != MotelySearchStatus.Running)
            return;

        _pauseBarrier.SignalAndWait();
        _elapsedTime.Stop();
    }

    private void ReportSeed(ReadOnlySpan<char> seed)
    {
        FancyConsole.WriteLine($"{seed}");
    }

    private void PrintReport()
    {
        double elapsedMS = _elapsedTime.ElapsedMilliseconds;

        if (elapsedMS - _lastReportMS < 500) return;

        _lastReportMS = elapsedMS;

        int thisCompletedCount = _completedBatchCount - _startBatchIndex;

        double totalPortionFinished = _completedBatchCount / (double)_threads[0].MaxBatch;
        double thisPortionFinished = thisCompletedCount / (double)_threads[0].MaxBatch;
        double totalTimeEstimate = elapsedMS / thisPortionFinished;
        double timeLeft = totalTimeEstimate - elapsedMS;

        string timeLeftFormatted;
        bool invalid = double.IsNaN(timeLeft) || double.IsInfinity(timeLeft) || timeLeft < 0;
        // Clamp to max TimeSpan if too large - for very slow searches
        if (invalid || timeLeft > TimeSpan.MaxValue.TotalMilliseconds)
        {
            timeLeftFormatted = "--:--:--";
        }
        else
        {
            TimeSpan timeLeftSpan = TimeSpan.FromMilliseconds(Math.Min(timeLeft, TimeSpan.MaxValue.TotalMilliseconds));
            if (timeLeftSpan.Days == 0) timeLeftFormatted = $"{timeLeftSpan:hh\\:mm\\:ss}";
            else timeLeftFormatted = $"{timeLeftSpan:d\\:hh\\:mm\\:ss}";
        }

        // Calculate seeds per millisecond
        // Avoid divide by zero for a very fast find
        double seedsPerMS = 0;
        if (elapsedMS > 1)
            seedsPerMS = thisCompletedCount * (double)_threads[0].SeedsPerBatch / elapsedMS;

        FancyConsole.SetBottomLine($"{Math.Round(totalPortionFinished * 100, 2):F2}% ~{timeLeftFormatted} remaining ({Math.Round(seedsPerMS)} seeds/ms)");

    }

    public void Dispose()
    {
        Pause();

        // Atomically replace paused state with Disposed state

        MotelySearchStatus oldStatus = Interlocked.Exchange(ref _status, MotelySearchStatus.Disposed);

        if (oldStatus == MotelySearchStatus.Paused)
        {
            _unpauseBarrier.SignalAndWait();
        }
        else
        {
            Debug.Assert(oldStatus == MotelySearchStatus.Completed);
        }


        foreach (MotelySearchThread thread in _threads)
        {
            thread.Dispose();
        }

        Marshal.FreeHGlobal((nint)_pseudoHashKeyLengths);
        Marshal.FreeHGlobal((nint)_pseudoHashReverseMap);

        GC.SuppressFinalize(this);
    }

    ~MotelySearch()
    {
        if (_status != MotelySearchStatus.Disposed)
        {
            Dispose();
        }
    }

    private abstract class MotelySearchThread : IDisposable
    {
        public readonly MotelySearch<TFilter> Search;
        public readonly int ThreadIndex;
        public readonly Thread Thread;

        public int MaxBatch { get; internal set; }
        public int SeedsPerBatch { get; internal set; }

        public MotelySearchThread(MotelySearch<TFilter> search, int threadIndex)
        {
            Search = search;
            ThreadIndex = threadIndex;

            Thread = new(ThreadMain)
            {
                Name = $"Motely Search Thread {ThreadIndex}"
            };

            Thread.Start();
        }

        private void ThreadMain()
        {
            while (true)
            {

                switch (Search._status)
                {
                    case MotelySearchStatus.Paused:
                        Search._pauseBarrier.SignalAndWait();
                        // ...Paused
                        Search._unpauseBarrier.SignalAndWait();
                        continue;

                    case MotelySearchStatus.Completed:
                        Debug.Assert(Search._batchIndex >= MaxBatch);
                        return;

                    case MotelySearchStatus.Disposed:
                        return;
                }

                int batchIdx = Interlocked.Increment(ref Search._batchIndex);

                if (batchIdx > MaxBatch)
                {
                    Search._batchIndex = MaxBatch;
                    Search._status = MotelySearchStatus.Completed;
                    return;
                }

                SearchBatch(batchIdx);

                Interlocked.Increment(ref Search._completedBatchCount);

                Search.PrintReport();
            }

        }

        protected abstract void SearchBatch(int batchIdx);

        public void Dispose()
        {
            Thread.Join();
        }
    }

    private sealed unsafe class MotelyProviderSearchThread : MotelySearchThread
    {
        public readonly IMotelySeedProvider SeedProvider;

        private readonly Vector512<double>* _hashCache;
        private readonly Vector512<double>* _seedCharacterMatrix;

        public MotelyProviderSearchThread(MotelySearch<TFilter> search, MotelySearchSettings<TFilter> settings, int index) : base(search, index)
        {

            if (settings.SeedProvider == null)
                throw new ArgumentException("Cannot create a provider search without a seed provider.");

            SeedProvider = settings.SeedProvider;

            MaxBatch = (SeedProvider.SeedCount + Vector512<double>.Count - 1) / Vector512<double>.Count;
            SeedsPerBatch = Vector512<double>.Count;

            _hashCache = (Vector512<double>*)Marshal.AllocHGlobal(sizeof(Vector512<double>) * Search._pseudoHashKeyLengthCount);
            _seedCharacterMatrix = (Vector512<double>*)Marshal.AllocHGlobal(sizeof(Vector512<double>) * Motely.MaxSeedLength);
        }

        protected override void SearchBatch(int batchIdx)
        {
            // If this is the last batch, check if we have enough seeds to fill a vector.
            if (batchIdx == MaxBatch && SeedProvider.SeedCount != MaxBatch * Vector512<double>.Count)
            {
                // If we don't, search the last seeds individually
                for (int i = 0; i < SeedProvider.SeedCount - (MaxBatch - 1) * Vector512<double>.Count; i++)
                {
                    SearchSingleSeed(SeedProvider.NextSeed());
                }

                return;
            }

            // The length of all the seeds
            int* seedLengths = stackalloc int[Vector512<double>.Count];

            // Are all the seeds the same length?
            bool homogeneousSeedLength = true;

            for (int seedIdx = 0; seedIdx < Vector512<double>.Count; seedIdx++)
            {
                ReadOnlySpan<char> seed = SeedProvider.NextSeed();

                seedLengths[seedIdx] = seed.Length;

                if (seedLengths[0] != seed.Length)
                    homogeneousSeedLength = false;

                for (int i = 0; i < seed.Length; i++)
                {
                    ((double*)_seedCharacterMatrix)[i * Vector512<double>.Count + seedIdx] = seed[i];
                }
            }


            if (homogeneousSeedLength)
            {
                // If all the seeds are the same length, we can be fast and vectorize!
                int seedLength = seedLengths[0];

                // Calculate the partial psuedohash cache
                for (int pseudohashKeyIdx = 0; pseudohashKeyIdx < Search._pseudoHashKeyLengthCount; pseudohashKeyIdx++)
                {
                    int pseudohashKeyLength = Search._pseudoHashKeyLengths[pseudohashKeyIdx];

                    Vector512<double> numVector = Vector512<double>.One;

                    for (int i = seedLength - 1; i >= 0; i--)
                    {
                        numVector = Vector512.Divide(Vector512.Create(1.1239285023), numVector);

                        numVector = Vector512.Multiply(numVector, _seedCharacterMatrix[i]);

                        numVector = Vector512.Multiply(numVector, Math.PI);
                        numVector = Vector512.Add(numVector, Vector512.Create((i + pseudohashKeyLength + 1) * Math.PI));

                        Vector512<double> intPart = Vector512.Floor(numVector);
                        numVector = Vector512.Subtract(numVector, intPart);
                    }

                    _hashCache[pseudohashKeyIdx] = numVector;
                }

                MotelySearchContextParams searchParams = new(
                    new(_hashCache, Search._pseudoHashReverseMap),
                    seedLength,
                    0, null,
                    _seedCharacterMatrix
                );

                MotelyVectorSearchContext searchContext = new(ref searchParams);

                VectorMask mask = Search._filter.Filter(ref searchContext);

                if (mask.Value != 0)
                {
                    Span<char> seed = stackalloc char[Motely.MaxSeedLength];

                    for (int i = 0; i < Vector512<double>.Count; i++)
                    {
                        if (mask[i])
                        {
                            for (int j = 0; j < seedLength; j++)
                            {
                                seed[j] = (char)((double*)_seedCharacterMatrix)[j * Vector512<double>.Count + i];
                            }

                            Search.ReportSeed(seed[..seedLength]);
                        }
                    }
                }
            }
            else
            {
                // Otherwise, we need to search all the seeds individually
                Span<char> seed = stackalloc char[Motely.MaxSeedLength];

                for (int i = 0; i < Vector512<double>.Count; i++)
                {
                    int seedLength = seedLengths[i];

                    for (int j = 0; j < seedLength; j++)
                    {
                        seed[j] = (char)((double*)_seedCharacterMatrix)[j * Vector512<double>.Count + i];
                    }

                    SearchSingleSeed(seed[..seedLength]);
                }

            }
        }

        private void SearchSingleSeed(ReadOnlySpan<char> seed)
        {
            char* seedLastCharacters = stackalloc char[Motely.MaxSeedLength - 1];

            // Calculate the partial psuedohash cache
            for (int pseudohashKeyIdx = 0; pseudohashKeyIdx < Search._pseudoHashKeyLengthCount; pseudohashKeyIdx++)
            {
                int pseudohashKeyLength = Search._pseudoHashKeyLengths[pseudohashKeyIdx];

                double num = 1;

                for (int i = seed.Length - 1; i >= 0; i--)
                {
                    num = (1.1239285023 / num * seed[i] * Math.PI + (i + pseudohashKeyLength + 1) * Math.PI) % 1;
                }

                _hashCache[pseudohashKeyIdx] = Vector512.Create(num);
            }

            for (int i = 0; i < seed.Length - 1; i++)
            {
                seedLastCharacters[i] = seed[i + 1];
            }

            Vector512<double> firstCharacterVector = Vector512.CreateScalar((double)seed[0]);

            MotelySearchContextParams searchParams = new(
                new(_hashCache, Search._pseudoHashReverseMap),
                seed.Length,
                seed.Length - 1,
                seedLastCharacters,
                &firstCharacterVector
            );

            MotelyVectorSearchContext searchContext = new(ref searchParams);

            VectorMask mask = Search._filter.Filter(ref searchContext);

            if (mask[0])
            {
                Search.ReportSeed(seed);
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            Marshal.FreeHGlobal((nint)_hashCache);
            Marshal.FreeHGlobal((nint)_seedCharacterMatrix);
        }
    }

    private sealed unsafe class MotelySequentialSearchThread : MotelySearchThread
    {
        // A cache of vectors containing all the seed's digits.
        private static readonly Vector512<double>[] SeedDigitVectors = new Vector512<double>[(Motely.SeedDigits.Length + Vector512<double>.Count - 1) / Vector512<double>.Count];

        static MotelySequentialSearchThread()
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

        private readonly int _batchCharCount;
        private readonly int _nonBatchCharCount;

        private readonly char* _digits;
        private readonly Vector512<double>* _hashes;

        public int LastCompletedBatch;

        public MotelySequentialSearchThread(MotelySearch<TFilter> search, MotelySearchSettings<TFilter> settings, int index) : base(search, index)
        {
            _digits = (char*)Marshal.AllocHGlobal(sizeof(char) * Motely.MaxSeedLength);

            _batchCharCount = settings.SequentialBatchCharacterCount;
            SeedsPerBatch = (int)Math.Pow(Motely.SeedDigits.Length, _batchCharCount);

            _nonBatchCharCount = Motely.MaxSeedLength - _batchCharCount;
            MaxBatch = (int)Math.Pow(Motely.SeedDigits.Length, _nonBatchCharCount);

            _hashes = (Vector512<double>*)Marshal.AllocHGlobal(sizeof(Vector512<double>) * Search._pseudoHashKeyLengthCount * (_batchCharCount + 1));
        }

        protected override void SearchBatch(int batchIdx)
        {
            // Figure out which digits this search is doing
            for (int i = _nonBatchCharCount - 1; i >= 0; i--)
            {
                int charIndex = batchIdx % Motely.SeedDigits.Length;
                _digits[Motely.MaxSeedLength - i - 1] = Motely.SeedDigits[charIndex];
                batchIdx /= Motely.SeedDigits.Length;
            }

            Vector512<double>* hashes = &_hashes[_batchCharCount * Search._pseudoHashKeyLengthCount];

            // Calculate hash for the first digits at all the required pseudohash lengths
            for (int pseudohashKeyIdx = 0; pseudohashKeyIdx < Search._pseudoHashKeyLengthCount; pseudohashKeyIdx++)
            {
                int pseudohashKeyLength = Search._pseudoHashKeyLengths[pseudohashKeyIdx];

                double num = 1;

                for (int i = Motely.MaxSeedLength - 1; i > _batchCharCount - 1; i--)
                {
                    num = (1.1239285023 / num * _digits[i] * Math.PI + (i + pseudohashKeyLength + 1) * Math.PI) % 1;
                }

                // We only need to write to the first lane because that's the only one that we need
                *(double*)&hashes[pseudohashKeyIdx] = num;
            }

            // Start searching
            for (int vectorIndex = 0; vectorIndex < SeedDigitVectors.Length; vectorIndex++)
            {
                SearchVector(_batchCharCount - 1, SeedDigitVectors[vectorIndex], hashes, 0);
            }
        }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void SearchVector(int i, Vector512<double> seedDigitVector, Vector512<double>* nums, int numsLaneIndex)
        {
            Vector512<double>* hashes = &_hashes[i * Search._pseudoHashKeyLengthCount];
            
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
                    Motely.MaxSeedLength, Motely.MaxSeedLength - 1, &_digits[1], &seedDigitVector
                );

                MotelyVectorSearchContext searchContext = new(ref searchContextParams);
                uint successMask = Search._filter.Filter(ref searchContext).Value;

                if (successMask != 0)
                {
                    Span<char> seed = stackalloc char[Motely.MaxSeedLength];

                    for (int lane = 0; lane < Vector512<double>.Count; lane++)
                    {
                        if ((successMask & 1) != 0)
                        {
                            _digits[0] = (char)seedDigitVector[lane];

                            for (int digit = 0; digit < Motely.MaxSeedLength; digit++)
                            {

                                if (_digits[digit] != '\0')
                                    seed[digit] = _digits[digit];
                            }

                            Search.ReportSeed(seed);
                        }

                        successMask >>= 1;
                    }
                }
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

        public new void Dispose()
        {
            base.Dispose();
            Marshal.FreeHGlobal((nint)_digits);
            Marshal.FreeHGlobal((nint)_hashes);
        }
    }
}