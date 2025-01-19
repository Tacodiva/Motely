
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public struct MotelyVectorPrngStream(Vector512<double> state)
{
    public Vector512<double> State = state;
}

public unsafe ref partial struct MotelyVectorSearchContext(Vector512<double>* seedHashes, int* seedHashesLookup)
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
    public void IteratePrngStream(ref MotelyVectorPrngStream stream)
    {
        stream.State = IteratePRNG(stream.State);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public void IteratePrngStream(ref MotelyVectorPrngStream stream, in Vector512<double> mask)
    {
        stream.State = Vector512.ConditionalSelect(mask, IteratePRNG(stream.State), stream.State);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePseudoSeed(ref MotelyVectorPrngStream stream)
    {
        IteratePrngStream(ref stream);
        return (stream.State + _seedHashes[0]) / Vector512.Create<double>(2);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePseudoSeed(ref MotelyVectorPrngStream stream, in Vector512<double> mask)
    {
        IteratePrngStream(ref stream, mask);
        return (stream.State + _seedHashes[0]) / Vector512.Create<double>(2);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePrngRandom(ref MotelyVectorPrngStream stream)
    {
        return VectorLuaRandomSingle.Random(IteratePseudoSeed(ref stream));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePrngRandom(ref MotelyVectorPrngStream stream, in Vector512<double> mask)
    {
        return VectorLuaRandomSingle.Random(IteratePseudoSeed(ref stream, mask));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector256<int> IteratePrngRandomInt(ref MotelyVectorPrngStream stream, int min, int max)
    {
        return VectorLuaRandomSingle.RandInt(IteratePseudoSeed(ref stream), min, max);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector256<int> IteratePrngRandomInt(ref MotelyVectorPrngStream stream, int min, int max, in Vector512<double> mask)
    {
        return VectorLuaRandomSingle.RandInt(IteratePseudoSeed(ref stream), min, max);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorEnum256<T> IteratePrngRandElement<T>(ref MotelyVectorPrngStream stream, T[] choices) where T : unmanaged, Enum
    {
        return VectorEnum256.Create(IteratePrngRandomInt(ref stream, 0, choices.Length), choices);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorEnum256<T> IteratePrngRandElement<T>(ref MotelyVectorPrngStream stream, T[] choices, in Vector512<double> mask) where T : unmanaged, Enum
    {
        return VectorEnum256.Create(IteratePrngRandomInt(ref stream, 0, choices.Length, mask), choices);
    }
}