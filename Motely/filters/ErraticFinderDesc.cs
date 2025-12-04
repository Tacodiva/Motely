using System.Runtime.Intrinsics;

namespace Motely;

public struct ErraticFinderDesc() : IMotelySeedFilterDesc<ErraticFinderDesc.FilterStruct>
{

    public const MotelyPlayingCardSuit CardSuit = MotelyPlayingCardSuit.Heart;
    public const int RequiredCount = 32;

    public FilterStruct CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        ctx.CacheErraticDeckPrngStream();

        return new FilterStruct();
    }

    public struct FilterStruct() : IMotelySeedFilter
    {

        public VectorMask Filter(ref MotelyVectorSearchContext searchContext)
        {
            var stream = searchContext.CreateErraticDeckPrngStream(true);

            Vector256<int> counts = Vector256<int>.Zero;

            for (int i = 0; i < 52; i++)
            {
                var cardVector = searchContext.GetNextErraticDeckCard(ref stream);

                counts += Vector256.ConditionalSelect(
                    VectorEnum256.Equals(cardVector.PlayingCardSuit, CardSuit),
                    Vector256<int>.One, Vector256<int>.Zero
                );
            }

            return Vector256.GreaterThanOrEqual(counts, Vector256.Create(RequiredCount));
        }
    }
}
