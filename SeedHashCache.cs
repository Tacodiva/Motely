
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;


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
