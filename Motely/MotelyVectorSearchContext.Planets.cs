using System.Runtime.CompilerServices;

namespace Motely;

ref partial struct MotelyVectorSearchContext
{
#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyVectorPrngStream CreatePlanetStreamCached(int ante, string source)
    {
        return CreatePrngStream(MotelyPrngKeys.Planet + source + ante, true);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyVectorPrngStream CreatePlanetStream(int ante, string source)
    {
        return CreatePrngStream(MotelyPrngKeys.Planet + source + ante);
    }

    // Helper method for filtering planet cards in vector context
    public VectorMask FilterPlanetCard(int ante, MotelyPlanetCard targetPlanet, string source = MotelyPrngKeys.ShopItemSource)
    {
        var planetStream = CreatePlanetStreamCached(ante, source);
        var planetChoices = MotelyEnum<MotelyPlanetCard>.Values;
        var planets = GetNextRandomElement(ref planetStream, planetChoices);
        return VectorEnum256.Equals(planets, targetPlanet);
    }
}