
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public struct MotelyVectorPrngStream(Vector512<double> state)
{
    public Vector512<double> State = state;
}

public ref struct MotelyVectorResampleStream(MotelyVectorPrngStream initialPrngStream, bool isCached)
{
    public const int StackResampleCount = 8;

    [InlineArray(StackResampleCount)]
    public struct MotelyResampleStreams
    {
        public MotelyVectorPrngStream PrngStream;
    }

    public MotelyVectorPrngStream InitialPrngStream = initialPrngStream;
    public MotelyResampleStreams ResamplePrngStreams;
    public int ResamplePrngStreamInitCount;
    public List<object>? HighResamplePrngStreams;
    public bool IsCached = isCached;
}

public delegate bool MotelyIndividualSeedSearcher(ref MotelySingleSearchContext searchContext);

internal unsafe struct MotelySearchContextParams(in SeedHashCache seedHashCache, int seedLength, int firstCharactersLength, char* seedFirstCharacters, Vector512<double>* seedLastCharacters, bool isAdditionalFilter = false)
{
    public SeedHashCache SeedHashCache = seedHashCache;
    public readonly int SeedLength = seedLength;
    public readonly int SeedFirstCharactersLength = firstCharactersLength;
    public readonly int SeedLastCharactersLength => SeedLength - SeedFirstCharactersLength;
    // The first characters which are the same between all vector lanes
    public readonly char* SeedFirstCharacters = seedFirstCharacters;
    // The last characters which are different between vector lanes
    public readonly Vector512<double>* SeedLastCharacters = seedLastCharacters;
    public readonly bool IsAdditionalFilter = isAdditionalFilter;

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly bool IsLaneValid(int lane)
    {
        // If all the lanes are the same seed, we say only the first lane is valid
        if (SeedFirstCharactersLength == SeedLength)
            return lane == 0;

        // Otherwise, the lane is valid if its character is not null
        return ((double*)&SeedLastCharacters[0])[lane] != '\0';
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly string GetSeed(int lane)
    {
        char* seed = stackalloc char[Motely.MaxSeedLength];
        int length = GetSeed(lane, seed);
        return new string(seed, 0, length);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly int GetSeed(int lane, char* output)
    {
        Debug.Assert(IsLaneValid(lane));

        int i = 0;

        for (; i < SeedLastCharactersLength; i++)
        {
            output[i] = (char)((double*)SeedLastCharacters)[i * Vector512<double>.Count + lane];
        }

        for (; i < SeedLength; i++)
        {
            output[i] = SeedFirstCharacters[i - SeedLastCharactersLength];
        }

        return SeedLength;
    }
}

public unsafe ref partial struct MotelyVectorSearchContext
{
    private readonly ref readonly MotelySearchParameters _searchParameters;
    private readonly ref readonly MotelySearchContextParams _contextParams;

    public readonly MotelyStake Stake => _searchParameters.Stake;
    public readonly MotelyDeck Deck => _searchParameters.Deck;

    private readonly ref readonly SeedHashCache SeedHashCache => ref _contextParams.SeedHashCache;
    private readonly int SeedLength => _contextParams.SeedLength;
    private readonly char* SeedFirstCharacters => _contextParams.SeedFirstCharacters;
    private readonly int SeedFirstCharactersLength => _contextParams.SeedFirstCharactersLength;
    private readonly int SeedLastCharactersLength => _contextParams.SeedLastCharactersLength;
    private readonly Vector512<double>* SeedLastCharacters => _contextParams.SeedLastCharacters;
    private readonly bool IsAdditionalFilter => _contextParams.IsAdditionalFilter;

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    internal MotelyVectorSearchContext(ref readonly MotelySearchParameters searchParameters, ref readonly MotelySearchContextParams contextParams)
    {
        _contextParams = ref contextParams;
        _searchParameters = ref searchParameters;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsLaneValid(int lane) => _contextParams.IsLaneValid(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly string GetSeed(int lane) => _contextParams.GetSeed(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetSeed(int lane, char* output) => _contextParams.GetSeed(lane, output);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorMask SearchIndividualSeeds(MotelyIndividualSeedSearcher searcher)
    {
        uint results = 0;

        for (int lane = 0; lane < Vector512<double>.Count; lane++)
        {
            if (IsLaneValid(lane))
            {
                MotelySingleSearchContext singleSearchContext = new(in _searchParameters, in _contextParams, lane);

                bool success = searcher(ref singleSearchContext);

                if (success)
                {
                    results |= 1u << lane;
                }
            }
        }

        return new(results);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorMask SearchIndividualSeeds(VectorMask mask, MotelyIndividualSeedSearcher searcher)
    {

        if (mask.IsAllFalse()) return default;

        uint results = 0;

        uint maskShift = mask.Value;

        for (int lane = 0; lane < Vector512<double>.Count; lane++)
        {
            if ((maskShift & 1) != 0 && IsLaneValid(lane))
            {
                MotelySingleSearchContext singleSearchContext = new(in _searchParameters, in _contextParams, lane);

                bool success = searcher(ref singleSearchContext);

                if (success)
                {
                    results |= 1u << lane;
                }
            }

            maskShift >>= 1;
        }

        return new(results);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly Vector512<double> PseudoHashCached(string key)
    {
        if (IsAdditionalFilter)
        {
            // If we are an additional filter, we can't guarantee that our cached pseudo hashes are actually cached
            if (!SeedHashCache.HasPartialHash(key.Length))
                return InternalPseudoHash(key);
        }

#if DEBUG
        if (!SeedHashCache.HasPartialHash(key.Length))
            throw new KeyNotFoundException("Cache does not contain key :c");
#endif

        return InternalPseudoHashCached(key);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private readonly Vector512<double> InternalPseudoHashCached(string key)
    {
        Vector512<double> calcVector = SeedHashCache.GetPartialHashVector(key.Length);

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
    public readonly Vector512<double> PseudoHash(string key)
    {
        if (SeedHashCache.HasPartialHash(key.Length))
        {
            return InternalPseudoHashCached(key);
        }

        return InternalPseudoHash(key);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private readonly Vector512<double> InternalPseudoHash(string key)
    {
        int seedLastCharacterLength = SeedLastCharactersLength;
        double num = 1;

        // First we do the first characters of the seed which are the same between all vector lanes
        for (int i = SeedFirstCharactersLength - 1; i >= 0; i--)
        {
            num = (1.1239285023 / num * SeedFirstCharacters[i] * Math.PI + Math.PI * (i + key.Length + seedLastCharacterLength + 1)) % 1;
        }

        // Then we vectorize and do the last characters of the seed
        Vector512<double> numVector = Vector512.Create(num);

        for (int i = seedLastCharacterLength - 1; i >= 0; i--)
        {
            numVector = Vector512.Divide(Vector512.Create(1.1239285023), numVector);

            numVector = Vector512.Multiply(numVector, SeedLastCharacters[i]);

            numVector = Vector512.Multiply(numVector, Math.PI);
            numVector = Vector512.Add(numVector, Vector512.Create((i + key.Length + 1) * Math.PI));

            Vector512<double> intPart = Vector512.Floor(numVector);
            numVector = Vector512.Subtract(numVector, intPart);
        }

        // Now finally the actual key
        for (int i = key.Length - 1; i >= 0; i--)
        {
            numVector = Vector512.Divide(Vector512.Create(1.1239285023), numVector);

            numVector = Vector512.Multiply(numVector, key[i]);

            numVector = Vector512.Multiply(numVector, Math.PI);
            numVector = Vector512.Add(numVector, Vector512.Create((i + 1) * Math.PI));

            Vector512<double> intPart = Vector512.Floor(numVector);
            numVector = Vector512.Subtract(numVector, intPart);
        }

        return numVector;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static Vector512<double> IteratePRNG(Vector512<double> state)
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
    public MotelyVectorPrngStream CreatePrngStreamCached(string key)
    {
        return new(PseudoHashCached(key));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVectorPrngStream CreatePrngStream(string key)
    {
        return new(PseudoHash(key));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> GetNextPrngState(ref MotelyVectorPrngStream stream)
    {
        return stream.State = IteratePRNG(stream.State);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> GetNextPrngState(ref MotelyVectorPrngStream stream, in Vector512<double> mask)
    {
        return stream.State = Vector512.ConditionalSelect(mask, IteratePRNG(stream.State), stream.State);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePseudoSeed(ref MotelyVectorPrngStream stream)
    {
        return (GetNextPrngState(ref stream) + SeedHashCache.GetSeedHashVector()) / Vector512.Create<double>(2);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePseudoSeed(ref MotelyVectorPrngStream stream, in Vector512<double> mask)
    {
        return (GetNextPrngState(ref stream, mask) + SeedHashCache.GetSeedHashVector()) / Vector512.Create<double>(2);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> GetNextPseudoSeed(ref MotelyVectorPrngStream stream, in Vector512<double> mask)
    {
        return (GetNextPrngState(ref stream, mask) + SeedHashCache.GetSeedHashVector()) / Vector512.Create<double>(2);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> GetNextRandom(ref MotelyVectorPrngStream stream)
    {
        return VectorLuaRandom.Random(IteratePseudoSeed(ref stream));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> GetNextRandom(ref MotelyVectorPrngStream stream, in Vector512<double> mask)
    {
        return VectorLuaRandom.Random(GetNextPseudoSeed(ref stream, mask));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector256<int> GetNextRandomInt(ref MotelyVectorPrngStream stream, int min, int max)
    {
        return VectorLuaRandom.RandInt(IteratePseudoSeed(ref stream), min, max);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector256<int> GetNextRandomInt(ref MotelyVectorPrngStream stream, int min, int max, in Vector512<double> mask)
    {
        return VectorLuaRandom.RandInt(IteratePseudoSeed(ref stream, mask), min, max);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorEnum256<T> GetNextRandomElement<T>(ref MotelyVectorPrngStream stream, T[] choices) where T : unmanaged, Enum
    {
        return VectorEnum256.Create(GetNextRandomInt(ref stream, 0, choices.Length), choices);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorEnum256<T> GetNextRandomElement<T>(ref MotelyVectorPrngStream stream, T[] choices, in Vector512<double> mask) where T : unmanaged, Enum
    {
        return VectorEnum256.Create(GetNextRandomInt(ref stream, 0, choices.Length, mask), choices);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyVectorPrngStream CreateResamplePrngStream(string key, int resample)
    {
        return CreatePrngStream(key + MotelyPrngKeys.Resample + (resample + 2));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyVectorPrngStream CreateResamplePrngStreamCached(string key, int resample)
    {
        // We don't cache resamples > 8 because they'd use an extra digit
        if (resample < 8) return CreatePrngStreamCached(key + MotelyPrngKeys.Resample + (resample + 2));
        return CreateResamplePrngStream(key, resample);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyVectorResampleStream CreateResampleStreamCached(string key)
    {
        return new(CreatePrngStreamCached(key), true);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyVectorResampleStream CreateResampleStream(string key)
    {
        return new(CreatePrngStream(key), false);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private ref MotelyVectorPrngStream GetResamplePrngStream(ref MotelyVectorResampleStream resampleStream, string key, int resample)
    {

        if (resample < MotelyVectorResampleStream.StackResampleCount)
        {
            ref MotelyVectorPrngStream prngStream = ref resampleStream.ResamplePrngStreams[resample];

            if (resample == resampleStream.ResamplePrngStreamInitCount)
            {
                ++resampleStream.ResamplePrngStreamInitCount;

                if (resampleStream.IsCached) prngStream = CreateResamplePrngStreamCached(key, resample);
                else prngStream = CreateResamplePrngStream(key, resample);
            }

            return ref prngStream;
        }

        {
            if (resample == MotelyVectorResampleStream.StackResampleCount)
            {
                resampleStream.HighResamplePrngStreams = [];
            }

            Debug.Assert(resampleStream.HighResamplePrngStreams != null);

            if (resample < resampleStream.HighResamplePrngStreams.Count)
            {
                return ref Unsafe.Unbox<MotelyVectorPrngStream>(resampleStream.HighResamplePrngStreams[resample]);
            }

            object prngStreamObject = new MotelyVectorPrngStream();

            resampleStream.HighResamplePrngStreams.Add(prngStreamObject);

            ref MotelyVectorPrngStream prngStream = ref Unsafe.Unbox<MotelyVectorPrngStream>(prngStreamObject);

            if (resampleStream.IsCached) prngStream = CreateResamplePrngStreamCached(key, resample);
            else prngStream = CreateResamplePrngStream(key, resample);

            return ref prngStream;
        }
    }
}