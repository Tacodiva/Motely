
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public struct MotelyVectorPrngStream(Vector512<double> state)
{
    public Vector512<double> State = state;
}

public ref struct MotelyVectorResampleStream(MotelyVectorPrngStream initialPrngStream)
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
}

public delegate bool MotelyIndividualSeedSearcher(ref MotelySingleSearchContext searchContext);

public unsafe ref partial struct MotelyVectorSearchContext
{
    private SeedHashCache _seedHashCache;

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    internal MotelyVectorSearchContext(in SeedHashCache seedHashCache)
    {
        _seedHashCache = seedHashCache;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorMask SearchIndividualSeeds(VectorMask mask, MotelyIndividualSeedSearcher searcher)
    {
        uint results = 0;

        uint maskShift = mask.Value;

        for (int lane = 0; lane < Vector512<double>.Count; lane++)
        {
            if ((maskShift & 1) != 0)
            {
                MotelySingleSearchContext singleSearchContext = new(lane, _seedHashCache);

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
    public Vector512<double> PseudoHash(string key)
    {
#if MOTELY_SAFE
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(key.Length, Motely.MaxPseudoHashKeyLength);
#endif
        Vector512<double> calcVector = _seedHashCache.GetPartialHashVector(key.Length);

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
        return (GetNextPrngState(ref stream) + _seedHashCache.GetSeedHashVector()) / Vector512.Create<double>(2);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> GetNextPseudoSeed(ref MotelyVectorPrngStream stream, in Vector512<double> mask)
    {
        return (GetNextPrngState(ref stream, mask) + _seedHashCache.GetSeedHashVector()) / Vector512.Create<double>(2);
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
        return VectorLuaRandom.RandInt(IteratePseudoSeed(ref stream), min, max);
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
    private MotelyVectorResampleStream CreateResampleStream(string key)
    {
        return new(CreatePrngStream(key));
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
                prngStream = CreateResamplePrngStream(key, resample);
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

            prngStream = CreateResamplePrngStream(key, resample);

            return ref prngStream;
        }
    }
}