
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

            // MotelyVectorJokerStream jokerStream = searchContext.CreateShopJokerStream(1);

            // for (int i = 0; i < 10; i++)
            //     Console.WriteLine(searchContext.GetNextJoker(ref jokerStream));

            // return VectorMask.NoBitsSet;


            return searchContext.SearchIndividualSeeds((ref MotelySingleSearchContext searchContext) =>
            {
                // Console.WriteLine($"\n{searchContext.GetSeed()}\n");

                var stream = searchContext.CreatePurpleSealTarotStream(1);

                for (int i = 0; i < 5; i++) {
                    Console.WriteLine(searchContext.GetNextTarot(ref stream));
                }


                // for (int i = 0; i < 30; i++) 

                // for (int i = 0; i < 5; i++)
                // {
                //     Console.WriteLine(searchContext.GetNextPrngState(ref stream));
                // }

                // MotelySinglePrngStream blackHoleStream = searchContext.CreatePrngStream(MotelyPrngKeys.SpectralSoulBlackHole + MotelyPrngKeys.Spectral + 6);

                // Console.WriteLine(searchContext.GetNextRandom(ref blackHoleStream));
                // Console.WriteLine(searchContext.GetNextRandom(ref blackHoleStream));

                // MotelySingleJokerStream jokerStream = searchContext.CreateShopJokerStream(1);

                // for (int i = 0; i < 10; i++)
                //     Console.WriteLine(searchContext.GetNextJoker(ref jokerStream));

                return false;
            });
        }
    }
}
