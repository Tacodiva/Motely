using System.Runtime.Intrinsics;

namespace Motely;

public struct LuckCardFilterDesc() : IMotelySeedFilterDesc<LuckCardFilterDesc.LuckyCardFilter>
{

    public LuckyCardFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        ctx.CachePseudoHash("lucky_money");
        // ctx.CachePseudoHash("space");
        return new LuckyCardFilter();
    }

    public struct LuckyCardFilter() : IMotelySeedFilter
    {

        public VectorMask Filter(ref MotelyVectorSearchContext searchContext)
        {
            // MotelyVectorPrngStream luckyMoney = searchContext.CreatePrngStreamCached("lucky_money");
            MotelyVectorPrngStream luckyMoney = searchContext.CreatePrngStream("lucky_money");
            // MotelyVectorPrngStream luckyMoney = searchContext.CreatePrngStreamCached("space");

            VectorMask mask = VectorMask.AllBitsSet;
            Vector512<double> values;

            for (int i = 0; i < 15; i++)
            {
                values = searchContext.GetNextRandom(ref luckyMoney);

                // mask &= Vector512.LessThan(values, Vector512.Create(1d / 25d));
                mask &= Vector512.LessThan(values, Vector512.Create(1d / 4d));

                if (mask.IsAllFalse())
                {
                    break;
                }
            }

            return mask;
        }
    }
}
