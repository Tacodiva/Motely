using System.Runtime.Intrinsics;

namespace Motely;

public struct LuckyCardFilterDesc() : IMotelySeedFilterDesc<LuckyCardFilterDesc.LuckyCardFilter>
{

    public LuckyCardFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        ctx.CachePseudoHash("lucky_money");
        return new LuckyCardFilter();
    }

    public struct LuckyCardFilter() : IMotelySeedFilter
    {

        public VectorMask Filter(ref MotelyVectorSearchContext searchContext)
        {
            MotelyVectorPrngStream luckyMoney = searchContext.CreatePrngStream("lucky_money");

            VectorMask mask = VectorMask.AllBitsSet;
            Vector512<double> values;

            for (int i = 0; i < 7; i++)
            {
                values = searchContext.GetNextRandom(ref luckyMoney);

                mask &= Vector512.LessThan(values, Vector512.Create(1d / 25d));

                if (mask.IsAllFalse())
                {
                    break;
                }
            }

            return mask;
        }
    }
}
