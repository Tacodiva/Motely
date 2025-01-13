using System.Runtime.Intrinsics;

namespace Motely;

public struct NaNSeedFilterDesc(string pseudoHashKey) : IMotelySeedFilterDesc<NaNSeedFilterDesc.NaNSeedFilter>
{
    public string PseudoHashKey { get; set; } = pseudoHashKey;

    public NaNSeedFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        ctx.RegisterPseudoHash(PseudoHashKey);
        return new NaNSeedFilter(PseudoHashKey);
    }

    public struct NaNSeedFilter(string pseudoHashKey) : IMotelySeedFilter
    {
        public readonly string PseudoHashKey = pseudoHashKey;

        public Vector512<double> Filter(ref MotelySearchContext searchContext)
        {
            Vector512<double> prng = searchContext.IteratePRNG(searchContext.PseudoHash(PseudoHashKey));
            return ~Vector512.Equals(prng, prng);
        }
    }
}
