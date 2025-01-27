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
            // return searchContext.SearchIndividualSeeds((ref MotelySingleSearchContext searchContext) =>
            // {
            //     MotelySingleTagStream tagStream;

            //     for (int ante = 2; ante <= 4; ante++)
            //     {
            //         tagStream = searchContext.CreateTagStream(ante);

            //         // Small blind
            //         if (searchContext.GetNextTag(ref tagStream) != MotelyTag.NegativeTag)
            //             return false;

            //         // Big blind
            //         if (searchContext.GetNextTag(ref tagStream) != MotelyTag.NegativeTag)
            //             return false;
            //     }

            //     return true;
            MotelyVectorTagStream tagStream;
            VectorMask mask = VectorMask.AllBitsSet;

            for (int ante = 2; ante <= 4; ante++)
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
        }
    }
}
