using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public struct NegativeTagFilterDesc() : IMotelySeedFilterDesc<NegativeTagFilterDesc.NegativeTagFilter>
{

    public NegativeTagFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        for (int ante = 2; ante <= 5; ante++)
            ctx.CacheTagStream(ante);

        return new NegativeTagFilter();
    }

    public struct NegativeTagFilter() : IMotelySeedFilter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorMask Filter(ref MotelyVectorSearchContext searchContext)
        {

            MotelyVectorTagStream tagStream;
            VectorMask mask = VectorMask.AllBitsSet;

            for (int ante = 2; ante <= 5; ante++)
            {
                tagStream = searchContext.CreateTagStream(ante);

                // Small blind
                mask &= VectorEnum256.Equals(searchContext.GetNextTag(ref tagStream), MotelyTag.NegativeTag);

                if (mask.IsAllFalse()) break;

                // Big blind
                mask &= VectorEnum256.Equals(searchContext.GetNextTag(ref tagStream), MotelyTag.NegativeTag);

                if (mask.IsAllFalse()) break;
            }

            return mask;

            // MotelyVectorTagStream tagStream;
            // Vector256<int> counts = Vector256<int>.Zero;

            // for (int ante = 2; ante <= 8; ante++)
            // {
            //     tagStream = searchContext.CreateTagStream(ante);

            //     // Small blind
            //     counts += Vector256<int>.One & VectorEnum256.Equals(searchContext.GetNextTag(ref tagStream), MotelyTag.NegativeTag);

            //     // if (mask.IsAllFalse()) break;

            //     // Big blind
            //     counts += Vector256<int>.One & VectorEnum256.Equals(searchContext.GetNextTag(ref tagStream), MotelyTag.NegativeTag);
            // }


            // return Vector256.GreaterThanOrEqual(counts, Vector256.Create(10));
        }
    }
}
