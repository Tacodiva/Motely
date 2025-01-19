using System.Runtime.Intrinsics;

namespace Motely;

public struct LuckyCardFilterDesc() : IMotelySeedFilterDesc<LuckyCardFilterDesc.LuckyCardFilter>
{

    public LuckyCardFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        ctx.RegisterPseudoRNG("lucky_money");
        return new LuckyCardFilter();
    }

    public struct LuckyCardFilter() : IMotelySeedFilter
    {

        public Vector512<double> Filter(ref MotelyVectorSearchContext searchContext)
        {

            MotelyVectorPrngStream luckyMoney = searchContext.CreatePrngStream("lucky_money");

            Vector512<double> mask = Vector512<double>.AllBitsSet;
            Vector512<double> values;

            for (int i = 0; i < 7; i++)
            {
                values = searchContext.IteratePrngRandom(ref luckyMoney);

                mask &= Vector512.LessThan(values, Vector512.Create(1d / 25d));

                if (Vector512.EqualsAll(mask, Vector512<double>.Zero))
                {
                    break;
                }
            }

            return mask;
        }
    }
}
