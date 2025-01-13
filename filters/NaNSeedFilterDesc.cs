using System.Runtime.Intrinsics;

namespace Motely;

public class NaNSeedFilterDesc(string pseudoHashKey) : IMotelySeedFilterDesc
{
    public string PseudoHashKey { get; set; } = pseudoHashKey;

    public IMotelySeedFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        ctx.RegisterPseudoHash(PseudoHashKey);
        return new NaNSeedFilter(PseudoHashKey);
    }

    public class NaNSeedFilter(string pseudoHashKey) : IMotelySeedFilter
    {
        public readonly string PseudoHashKey = pseudoHashKey;

        public Vector512<double> Filter(ref MotelySearchContext searchContext)
        {
            Vector512<double> prng = searchContext.IteratePRNG(searchContext.PseudoHash(PseudoHashKey));
            return ~Vector512.Equals(prng, prng);
        }
    }
}
