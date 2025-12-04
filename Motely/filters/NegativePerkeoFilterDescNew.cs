
namespace Motely;

using System.Runtime.CompilerServices;

public struct NegativePerkeoFilterDescNew() : IMotelySeedFilterDesc<NegativePerkeoFilterDescNew.FilterStruct>
{

    public const int MinAnte = 1;
    public const int MaxAnte = 2;

    public FilterStruct CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        for (int ante = MinAnte; ante <= MaxAnte; ante++)
            ctx.CacheSoulJokerStream(ante, MotelyJokerFixedRarityStreamFlags.ExcludeJokerType | MotelyJokerFixedRarityStreamFlags.ExcludeStickers);

        return new FilterStruct();
    }

    public struct FilterStruct() : IMotelySeedFilter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorMask Filter(ref MotelyVectorSearchContext searchContext)
        {
            VectorMask seedMask = VectorMask.NoBitsSet;

            for (int ante = MinAnte; ante <= MaxAnte; ante++)
            {
                var jokerEditionSteam = searchContext.CreateSoulJokerStream(ante, MotelyJokerFixedRarityStreamFlags.ExcludeJokerType | MotelyJokerFixedRarityStreamFlags.ExcludeStickers, true);

                var editionVector = searchContext.GetNextJoker(ref jokerEditionSteam).Edition;

                VectorMask negativeMask = VectorEnum256.Equals(editionVector, MotelyItemEdition.Negative);

                if (negativeMask.IsAllFalse()) continue;

                seedMask |= searchContext.SearchIndividualSeeds(negativeMask, (ref MotelySingleSearchContext searchContext) =>
                {
                    var jokerTypeStream = searchContext.CreateSoulJokerStream(ante, MotelyJokerFixedRarityStreamFlags.ExcludeEdition | MotelyJokerFixedRarityStreamFlags.ExcludeStickers);
                    var jokerType = searchContext.GetNextJoker(ref jokerTypeStream).Type;

                    if (jokerType != MotelyItemType.Perkeo) return false;

                    // We need to check if this ante has the soul
                    MotelySingleTarotStream tarotStream = default;
                    MotelySingleSpectralStream spectralStream = default;
                    bool tarotStreamInit = false, spectralStreamInit = false;

                    MotelySingleBoosterPackStream boosterPackStream = searchContext.CreateBoosterPackStream(ante);

                    for (int i = 0; i < 5; i++)
                    {
                        MotelyBoosterPack pack = searchContext.GetNextBoosterPack(ref boosterPackStream);

                        if (pack.GetPackType() == MotelyBoosterPackType.Arcana)
                        {
                            if (!tarotStreamInit)
                            {
                                tarotStreamInit = true;
                                tarotStream = searchContext.CreateArcanaPackTarotStream(ante, true);
                            }

                            if (searchContext.GetNextArcanaPackHasTheSoul(ref tarotStream, pack.GetPackSize()))
                                return true;
                        }

                        if (pack.GetPackType() == MotelyBoosterPackType.Spectral)
                        {
                            if (!spectralStreamInit)
                            {
                                spectralStreamInit = true;
                                spectralStream = searchContext.CreateSpectralPackSpectralStream(ante, true);
                            }

                            if (searchContext.GetNextSpectralPackHasTheSoul(ref spectralStream, pack.GetPackSize()))
                                return true;
                        }
                    }

                    return false;

                });

            }

            return seedMask;
        }
    }
}
