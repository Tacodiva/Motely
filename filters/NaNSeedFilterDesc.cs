using System.Runtime.Intrinsics;

namespace Motely;

public struct NaNSeedFilterDesc(string pseudoHashKey) : IMotelySeedFilterDesc<NaNSeedFilterDesc.NaNSeedFilter>
{
    public string PseudoHashKey { get; set; } = pseudoHashKey;

    public NaNSeedFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        ctx.CachePseudoHash(PseudoHashKey);
        return new NaNSeedFilter(PseudoHashKey);
    }

    public struct NaNSeedFilter(string pseudoHashKey) : IMotelySeedFilter
    {
        public readonly string PseudoHashKey = pseudoHashKey;

        public VectorMask Filter(ref MotelyVectorSearchContext searchContext)
        {
            Vector512<double> prng = MotelyVectorSearchContext.IteratePRNG(searchContext.PseudoHash(PseudoHashKey));
            return ~Vector512.Equals(prng, prng);
        }
    }
}
