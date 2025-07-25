

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Motely;

public ref struct MotelySinglePlanetStream(string resampleKey, MotelySingleResampleStream resampleStream, MotelySinglePrngStream blackHoleStream)
{
    public readonly bool IsNull => ResampleKey == null;
    public readonly string ResampleKey = resampleKey;
    public MotelySingleResampleStream ResampleStream = resampleStream;
    public MotelySinglePrngStream BlackHolePrngStream = blackHoleStream;
    public readonly bool IsBlackHoleable => !BlackHolePrngStream.IsInvalid;
}

ref partial struct MotelySingleSearchContext
{
#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySinglePlanetStream CreatePlanetStream(string source, int ante, bool blackHoleable, bool isCached)
    {
        return new(
            MotelyPrngKeys.Planet + source + ante,
            CreateResampleStream(MotelyPrngKeys.Planet + source + ante, isCached),
            blackHoleable ?
                CreatePrngStream(MotelyPrngKeys.PlanetBlackHole + MotelyPrngKeys.Planet + ante, isCached) :
                MotelySinglePrngStream.Invalid
        );
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySinglePlanetStream CreateCelestialPackPlanetStream(int ante, bool isCached = false) =>
        CreatePlanetStream(MotelyPrngKeys.CelestialPackItemSource, ante, true, isCached);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySinglePlanetStream CreateShopPlanetStream(int ante, bool isCached = false) =>
        CreatePlanetStream(MotelyPrngKeys.ShopItemSource, ante, false, isCached);


    public MotelySingleItemSet GetNextCelestialPackContents(ref MotelySinglePlanetStream planetStream, MotelyBoosterPackSize size)
    {
        MotelySingleItemSet pack = new();
        int cardCount = MotelyBoosterPackType.Celestial.GetCardCount(size);

        for (int i = 0; i < cardCount; i++)
            pack.Append(GetNextPlanet(ref planetStream, pack));

        return pack;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItem GetNextPlanet(ref MotelySinglePlanetStream planetStream)
    {
        if (planetStream.IsBlackHoleable)
        {
            if (GetNextRandom(ref planetStream.BlackHolePrngStream) > 0.997)
            {
                return MotelyItemType.BlackHole;
            }
        }

        return (MotelyItemType)MotelyItemTypeCategory.PlanetCard |
            (MotelyItemType)GetNextRandomInt(ref planetStream.ResampleStream.InitialPrngStream, 0, MotelyEnum<MotelyPlanetCard>.ValueCount);
    }
    
    public MotelyItem GetNextPlanet(ref MotelySinglePlanetStream planetStream, in MotelySingleItemSet itemSet)
    {
        if (planetStream.IsBlackHoleable && !itemSet.Contains(MotelyItemType.BlackHole))
        {
            if (GetNextRandom(ref planetStream.BlackHolePrngStream) > 0.997)
            {
                return MotelyItemType.BlackHole;
            }
        }

        MotelyItemType planet = (MotelyItemType)MotelyItemTypeCategory.PlanetCard |
            (MotelyItemType)GetNextRandomInt(ref planetStream.ResampleStream.InitialPrngStream, 0, MotelyEnum<MotelyPlanetCard>.ValueCount);

        int resampleCount = 0;

        while (true)
        {
            if (!itemSet.Contains(planet))
            {
                return planet;
            }

            planet = (MotelyItemType)MotelyItemTypeCategory.PlanetCard | (MotelyItemType)GetNextRandomInt(
                ref GetResamplePrngStream(ref planetStream.ResampleStream, planetStream.ResampleKey, resampleCount),
                0, MotelyEnum<MotelyVoucher>.ValueCount
            );

            ++resampleCount;
        }
    }
}