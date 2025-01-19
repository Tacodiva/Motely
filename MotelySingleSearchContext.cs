
using System.Runtime.CompilerServices;

namespace Motely;

public struct MotelySinglePrngStream(double state)
{
    public double State = state;
}

public unsafe ref partial struct MotelySingleSearchContext
{
    public readonly int VectorLane;

    private SeedHashCache _seedHashCache;

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    internal MotelySingleSearchContext(int lane, in SeedHashCache hashCache)
    {
        VectorLane = lane;
        _seedHashCache = hashCache;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public double PseudoHash(string key)
    {
#if MOTELY_SAFE
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(key.Length, Motely.MaxPseudoHashKeyLength);
#endif

        double num = _seedHashCache.GetPartialHash(key.Length, VectorLane);

        for (int i = key.Length - 1; i >= 0; i--)
        {
            num = ((1.1239285023 / num) * key[i] * Math.PI + (i + 1) * Math.PI) % 1;
        }

        return num;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static double IteratePRNG(double state)
    {
        return Math.Round((state * 1.72431234 + 2.134453429141) % 1, 13);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySinglePrngStream CreatePrngStream(string key)
    {
        return new(PseudoHash(key));
    }

#if DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public void IteratePrngStream(ref MotelySinglePrngStream stream)
    {
        stream.State = IteratePRNG(stream.State);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public double IteratePseudoSeed(ref MotelySinglePrngStream stream)
    {
        IteratePrngStream(ref stream);
        return (stream.State + _seedHashCache.GetSeedHash(VectorLane)) / 2d;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public double IteratePrngRandom(ref MotelySinglePrngStream stream)
    {
        return LuaRandom.Random(IteratePseudoSeed(ref stream));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public int IteratePrngRandomInt(ref MotelySinglePrngStream stream, int min, int max)
    {
        return LuaRandom.RandInt(IteratePseudoSeed(ref stream), min, max);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public T IteratePrngRandElement<T>(ref MotelySinglePrngStream stream, T[] choices)
    {
        return choices[IteratePrngRandomInt(ref stream, 0, choices.Length)];
    }
}