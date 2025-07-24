
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

                MotelySingleBoosterPackStream packStream = searchContext.CreateBoosterPackStream(1);

                MotelySingleTarotStream tarotStream = searchContext.CreateArcanaPackTarotStream(1);
                MotelySinglePlanetStream planetStream = searchContext.CreateCelestialPackPlanetStream(1);

                for (int i = 0; i < 6; i++)
                {
                    MotelyBoosterPack pack = searchContext.GetNextBoosterPack(ref packStream);

                    Console.WriteLine(pack);

                    switch (pack.GetPackType())
                    {
                        case MotelyBoosterPackType.Arcana:
                            Console.WriteLine(searchContext.GetArcanaPackContents(ref tarotStream, pack.GetPackSize()).ToString());
                            break;
                        case MotelyBoosterPackType.Celestial:
                            Console.WriteLine(searchContext.GetCelestialPackContents(ref planetStream, pack.GetPackSize()).ToString());
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
