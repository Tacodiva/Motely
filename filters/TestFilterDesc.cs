
using System.Runtime.Intrinsics;

namespace Motely;

public struct TestFilterDesc() : IMotelySeedFilterDesc<TestFilterDesc.TestFilter>
{

    public TestFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        return new TestFilter();
    }

    public struct TestFilter() : IMotelySeedFilter
    {

        public VectorMask Filter(ref MotelyVectorSearchContext searchContext)
        {
            return searchContext.SearchIndividualSeeds((ref MotelySingleSearchContext searchContext) =>
            {

                MotelySingleShopItemStream shopItemStream = searchContext.CreateShopItemStream(3);

                for (int i = 0; i < 20; i++)
                {
                    MotelyItem item = searchContext.GetNextShopItem(ref shopItemStream);
                    Console.WriteLine(item);
                }

                return true;
            });
        }
    }
}
