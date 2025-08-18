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
            MotelyVectorPrngStream stream = searchContext.CreatePrngStream(PseudoHashKey);
            Vector512<double> prng = searchContext.GetNextPrngState(ref stream);
            return ~Vector512.Equals(prng, prng);
        }
    }
}
