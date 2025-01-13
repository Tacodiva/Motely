
using System.Runtime.Intrinsics;

namespace Motely;

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
