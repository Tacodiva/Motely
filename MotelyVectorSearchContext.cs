
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

internal unsafe struct MotelySearchContextParams(in SeedHashCache seedHashCache, int seedLength, char* seedLastCharacters, in Vector512<double> seedFirstCharacter)
{
    public SeedHashCache SeedHashCache = seedHashCache;
    public readonly int SeedLength = seedLength;
    public readonly char* SeedLastCharacters = seedLastCharacters;
    public readonly Vector512<double> SeedFirstCharacter = seedFirstCharacter;
}

public unsafe ref partial struct MotelyVectorSearchContext
{
    private ref MotelySearchContextParams _params;

    private ref SeedHashCache SeedHashCache => ref _params.SeedHashCache;
    private readonly int SeedLength => _params.SeedLength;
    private readonly char* SeedLastCharacters => _params.SeedLastCharacters;
    private readonly Vector512<double> SeedFirstCharacter => _params.SeedFirstCharacter;

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    internal MotelyVectorSearchContext(ref MotelySearchContextParams @params)
    {
        _params = ref @params;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorMask SearchIndividualSeeds(MotelyIndividualSeedSearcher searcher)
    {
        uint results = 0;

        for (int lane = 0; lane < Vector512<double>.Count; lane++)
        {
            MotelySingleSearchContext singleSearchContext = new(ref _params, lane);

            bool success = searcher(ref singleSearchContext);

            if (success)
            {
                results |= 1u << lane;
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
            if ((maskShift & 1) != 0)
            {
                MotelySingleSearchContext singleSearchContext = new(ref _params, lane);

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
    public Vector512<double> PseudoHashCached(string key)
    {
#if MOTELY_SAFE
        if (!_seedHashCache.HasPartialHash(key.Length))
            throw new KeyNotFoundException("Cache does not contain key :c");
#endif

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
    public Vector512<double> PseudoHash(string key)
    {
        if (SeedHashCache.HasPartialHash(key.Length))
        {
            return PseudoHashCached(key);
        }

        double num = 1;

        // First we do the first 7 digits of the seed which are the same between all vector lanes
        for (int i = SeedLength - 2; i >= 0; i--)
        {
            num = (1.1239285023 / num * SeedLastCharacters[i] * Math.PI + Math.PI * (i + key.Length + 2)) % 1;
        }

        // Then we vectorize and do the last digit of the seed
        Vector512<double> numVector = Vector512.Create(num);

        {
            numVector = Vector512.Divide(Vector512.Create(1.1239285023), numVector);

            numVector = Vector512.Multiply(numVector, SeedFirstCharacter);

            numVector = Vector512.Multiply(numVector, Math.PI);
            numVector = Vector512.Add(numVector, Vector512.Create((key.Length + 1) * Math.PI));

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