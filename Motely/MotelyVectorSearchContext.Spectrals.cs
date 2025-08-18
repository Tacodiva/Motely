using System.Runtime.CompilerServices;

namespace Motely;

ref partial struct MotelyVectorSearchContext
{
#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyVectorPrngStream CreateSpectralStreamCached(int ante, string source)
    {
        return CreatePrngStream(MotelyPrngKeys.Spectral + source + ante, true);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyVectorPrngStream CreateSpectralStream(int ante, string source)
    {
        return CreatePrngStream(MotelyPrngKeys.Spectral + source + ante);
    }

    // Helper method for filtering spectral cards in vector context
    public VectorMask FilterSpectralCard(int ante, MotelySpectralCard targetSpectral, string source = MotelyPrngKeys.ShopItemSource)
    {
        var spectralStream = CreateSpectralStreamCached(ante, source);
        var spectralChoices = MotelyEnum<MotelySpectralCard>.Values;
        var spectrals = GetNextRandomElement(ref spectralStream, spectralChoices);
        return VectorEnum256.Equals(spectrals, targetSpectral);
    }
}