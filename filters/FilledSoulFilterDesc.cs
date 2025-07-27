using System.Runtime.Intrinsics;

namespace Motely;

public struct FilledSoulFilterDesc() : IMotelySeedFilterDesc<FilledSoulFilterDesc.SoulFilter>
{

    public const int MinAnte = 5;
    public const int MaxAnte = 9;
    public const int SoulsInARow = 5;

    public SoulFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        for (int ante = MinAnte; ante <= MaxAnte; ante++)
        {
            ctx.CachePseudoHash(MotelyPrngKeys.TerrotSoul + MotelyPrngKeys.Tarot + ante);
        }

        return new SoulFilter();
    }

    public struct SoulFilter() : IMotelySeedFilter
    {

        public VectorMask Filter(ref MotelyVectorSearchContext searchContext)
        {
            VectorMask mask = VectorMask.NoBitsSet;

            for (int ante = MinAnte; ante <= MaxAnte; ante++)
            {
                MotelyVectorPrngStream vectorSoulStream = searchContext.CreatePrngStream(MotelyPrngKeys.TerrotSoul + MotelyPrngKeys.Tarot + ante);

                Vector512<double> soulPoll = searchContext.GetNextRandom(ref vectorSoulStream);

                VectorMask soulCards = Vector512.GreaterThan(soulPoll, Vector512.Create(0.997));

                mask |= searchContext.SearchIndividualSeeds(soulCards, (ref MotelySingleSearchContext searchContext) =>
                {

                    MotelySinglePrngStream singleSoulStream = vectorSoulStream.CreateSingleStream(searchContext.VectorLane);

                    for (int i = 0; i < SoulsInARow - 1; i++)
                    {
                        if (searchContext.GetNextRandom(ref singleSoulStream) <= 0.997)
                        {
                            return false;
                        }
                    }

                    return true;
                });
            }

            return mask;
        }
    }
}
