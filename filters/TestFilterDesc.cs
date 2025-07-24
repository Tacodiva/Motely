
using System.Runtime.Intrinsics;

namespace Motely;

public struct TestFilterDesc() : IMotelySeedFilterDesc<TestFilterDesc.TestFilter>
{

    public const MotelyShopStreamFlags ShopFlags =
        MotelyShopStreamFlags.ExcludeTarots
        | MotelyShopStreamFlags.ExcludePlanets;

    public const MotelyJokerStreamFlags JokerFlags =
        MotelyJokerStreamFlags.ExcludeStickers
        | MotelyJokerStreamFlags.ExcludeEdition
        | MotelyJokerStreamFlags.ExcludeCommonJokers
        | MotelyJokerStreamFlags.ExcludeUncommonJokers;

    public TestFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        ctx.CacheShopStream(1, ShopFlags, JokerFlags);

        return new TestFilter();
    }

    public struct TestFilter() : IMotelySeedFilter
    {

        public VectorMask Filter(ref MotelyVectorSearchContext searchContext)
        {
            return searchContext.SearchIndividualSeeds((ref MotelySingleSearchContext searchContext) =>
            {

                int ante = 2;

                MotelySingleBoosterPackStream packStream = searchContext.CreateBoosterPackStream(ante);

                MotelySingleTarotStream tarotStream = searchContext.CreateArcanaPackTarotStream(ante);
                MotelySinglePlanetStream planetStream = searchContext.CreateCelestialPackPlanetStream(ante);
                MotelySingleSpectralStream spectralStream = searchContext.CreateSpectralPackSpectralStream(ante);
                MotelySingleStandardCardStream standardCardStream = searchContext.CreateStandardPackCardStream(ante);

                for (int i = 0; i < 6; i++)
                {
                    MotelyBoosterPack pack = searchContext.GetNextBoosterPack(ref packStream);

                    Console.WriteLine(pack);

                    switch (pack.GetPackType())
                    {
                        case MotelyBoosterPackType.Arcana:
                            Console.WriteLine(searchContext.GetNextArcanaPackContents(ref tarotStream, pack.GetPackSize()).ToString());
                            break;
                        case MotelyBoosterPackType.Celestial:
                            Console.WriteLine(searchContext.GetNextCelestialPackContents(ref planetStream, pack.GetPackSize()).ToString());
                            break;
                        case MotelyBoosterPackType.Spectral:
                            Console.WriteLine(searchContext.GetNextSpectralPackContents(ref spectralStream, pack.GetPackSize()).ToString());
                            break;
                        case MotelyBoosterPackType.Standard:
                            Console.WriteLine(searchContext.GetNextStandardPackContents(ref standardCardStream, pack.GetPackSize()).ToString());
                            break;
                    }
                }


                // var stream = searchContext.CreateShopItemStream(1, ShopFlags, JokerFlags);

                // for (int i = 0; i < 2; i++)
                // {
                //     MotelyItem item = searchContext.GetNextShopItem(ref stream);

                //     if (item.Type != MotelyItemType.Blueprint)
                //         return false;
                // }

                return true;
            });
        }
    }
}
