
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Motely;


#if !DEBUG
[method:MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
internal unsafe struct PartialSeedHashCache : IDisposable
{
    // A map of pseudohash key length => pointer to cached partial hash
    public readonly Vector512<double>** Cache;

    // The initial cache, copied into Cache when this is reset if cache was modified
    public readonly Vector512<double>** InitialCache;

    // This is memory for dynamically cached hashes. Those are hashes which where calculated but
    //   not specified upon the creation of the filter.
    public readonly Vector512<double>* DynamicCacheMemory;
    public int DynamicCacheEntryCount;

    public PartialSeedHashCache(IInternalMotelySearch search, Vector512<double>* partialSeedHashes)
    {
        Cache = (Vector512<double>**)Marshal.AllocHGlobal(sizeof(Vector512<double>*) * Motely.MaxCachedPseudoHashKeyLength);
        InitialCache = (Vector512<double>**)Marshal.AllocHGlobal(sizeof(Vector512<double>*) * Motely.MaxCachedPseudoHashKeyLength);

        // Initialize the dynamic cache
        DynamicCacheMemory = (Vector512<double>*)Marshal.AllocHGlobal(sizeof(Vector512<double>) * (Motely.MaxCachedPseudoHashKeyLength - search.PseudoHashKeyLengthCount));
        DynamicCacheEntryCount = 0;

        // Initialize the initial cache 
        Unsafe.InitBlockUnaligned(InitialCache, 0, (uint)sizeof(Vector512<double>*) * Motely.MaxCachedPseudoHashKeyLength);
        for (int i = 0; i < search.PseudoHashKeyLengthCount; i++)
        {
            int pseudohashKeyLength = search.PseudoHashKeyLengths[i];
            InitialCache[pseudohashKeyLength] = &partialSeedHashes[i];
        }

        // Initialize the cache
        ResetCache();

    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private void ResetCache()
    {
        Unsafe.CopyBlock(Cache, InitialCache, (uint)sizeof(Vector512<double>*) * Motely.MaxCachedPseudoHashKeyLength);
        DynamicCacheEntryCount = 0;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public void Reset()
    {
        if (DynamicCacheEntryCount != 0)
        {
            ResetCache();
        }
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly bool HasPartialHash(int keyLength)
    {
        return keyLength < Motely.MaxCachedPseudoHashKeyLength && Cache[keyLength] != null;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly Vector512<double> GetSeedHashVector()
    {
        Debug.Assert(Cache[0] != null);
        return *Cache[0];
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
        Debug.Assert(HasPartialHash(keyLength));
        return *Cache[keyLength];
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly double GetPartialHash(int keyLength, int lane)
    {
        return GetPartialHashVector(keyLength)[lane];
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public void CachePartialHash(int keyLength, Vector512<double> partialHash)
    {
        Debug.Assert(keyLength < Motely.MaxCachedPseudoHashKeyLength);
        Debug.Assert(!HasPartialHash(keyLength));

        int dynamicEntryIndex = DynamicCacheEntryCount++;

        Vector512<double>* dynamicCacheMemory = &DynamicCacheMemory[dynamicEntryIndex];

        *dynamicCacheMemory = partialHash;
        Cache[keyLength] = dynamicCacheMemory;
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal((nint)Cache);
        Marshal.FreeHGlobal((nint)InitialCache);
        Marshal.FreeHGlobal((nint)DynamicCacheMemory);
    }
}
