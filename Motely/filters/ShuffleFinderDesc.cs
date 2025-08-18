using System;
using System.Collections;
using System.Diagnostics;

namespace Motely;

public struct ShuffleFinderFilterDesc() : IMotelySeedFilterDesc<ShuffleFinderFilterDesc.ShuffleFinderFilter>
{

    public ShuffleFinderFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        return new ShuffleFinderFilter();
    }

    public struct ShuffleFinderFilter() : IMotelySeedFilter
    {

        private static readonly MotelyPlayingCardRank[] straightRankOrder = [
            MotelyPlayingCardRank.Ace,
            MotelyPlayingCardRank.Two,
            MotelyPlayingCardRank.Three,
            MotelyPlayingCardRank.Four,
            MotelyPlayingCardRank.Five,
            MotelyPlayingCardRank.Six,
            MotelyPlayingCardRank.Seven,
            MotelyPlayingCardRank.Eight,
            MotelyPlayingCardRank.Nine,
            MotelyPlayingCardRank.Ten,
            MotelyPlayingCardRank.Jack,
            MotelyPlayingCardRank.Queen,
            MotelyPlayingCardRank.King,
            MotelyPlayingCardRank.Ace,
        ];

        public enum HandType
        {
            HighCard,
            Pair,
            TwoPair,
            ThreeOfAKind,
            Straight,
            Flush,
            FullHouse,
            FourOfAKind,
            StriaghtFlush,
        }

        public struct HandInfo(HandType type, int chips, int mult)
        {
            public HandType Type = type;
            public int Chips = chips, Mult = mult;

            public readonly double Score => Chips * Mult;

            public readonly double PlasmaScore
            {
                get
                {
                    double floor = Math.Floor((double)(Chips + Mult));
                    return floor * floor;
                }
            }
        }

        public static HandInfo BestScore(Span<MotelyItem> hand)
        {

            hand.Sort((a, b) => ((int)a.PlayingCardRank) - ((int)b.PlayingCardRank));

            int clubSuitCount = 0;
            int diamondSuitCount = 0;
            int heartSuitCount = 0;
            int spadeSuitCount = 0;

            int bestScore = 0;
            int bestScoreChips = 0, bestScoreMult = 0;
            HandType bestHand = HandType.HighCard;

            int[] cardCounts = new int[MotelyEnum<MotelyPlayingCardRank>.ValueCount];

            for (int i = 0; i < hand.Length; i++)
            {
                switch (hand[i].PlayingCardSuit)
                {
                    case MotelyPlayingCardSuit.Club:
                        ++clubSuitCount;
                        break;
                    case MotelyPlayingCardSuit.Diamond:
                        ++diamondSuitCount;
                        break;
                    case MotelyPlayingCardSuit.Heart:
                        ++heartSuitCount;
                        break;
                    case MotelyPlayingCardSuit.Spade:
                        ++spadeSuitCount;
                        break;
                }

                MotelyPlayingCardRank rank = hand[i].PlayingCardRank;

                int rankCount = ++cardCounts[(int)rank];

                int chips = 0, mult = 0;
                HandType handType = HandType.HighCard;

                switch (rankCount)
                {
                    case 1:
                        {
                            // High card
                            chips = 5 + GetCardChips(rank);
                            mult = 1;
                            handType = HandType.HighCard;
                            break;
                        }
                    case 2:
                        {
                            // Pair
                            chips = 10 + 2 * GetCardChips(rank);
                            mult = 2;
                            handType = HandType.Pair;
                            break;
                        }
                    case 3:
                        {
                            // Three of a kind
                            chips = 30 + 3 * GetCardChips(rank);
                            mult = 3;
                            handType = HandType.ThreeOfAKind;
                            break;
                        }
                    case 4:
                        {
                            // Four of a kind
                            chips = 60 + 4 * GetCardChips(rank);
                            mult = 7;
                            handType = HandType.FourOfAKind;
                            break;
                        }
                }

                if (mult * chips > bestScore)
                {
                    bestScoreChips = chips;
                    bestScoreMult = mult;
                    bestScore = bestScoreChips * bestScoreMult;
                    bestHand = handType;
                }
            }

            const int strightStartingCardCount = 10;

            bool[] straightStart = new bool[strightStartingCardCount];
            bool hasStraight = false;

            for (int i = 0; i < strightStartingCardCount; i++)
            {
                bool matches = true;

                for (int j = 0; j < 5; j++)
                {
                    if (cardCounts[(int)straightRankOrder[i + j]] == 0)
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    // Straight

                    straightStart[i] = true;
                    hasStraight |= true;

                    int chips = 30;

                    for (int j = 0; j < 5; j++)
                    {
                        chips += GetCardChips(straightRankOrder[i + j]);
                    }

                    if (chips * 4 > bestScore)
                    {
                        bestScoreChips = chips;
                        bestScoreMult = 4;
                        bestScore = bestScoreChips * bestScoreMult;
                        bestHand = HandType.Straight;
                    }
                }
            }

            void ScoreFlush(Span<MotelyItem> hand, MotelyPlayingCardSuit suit)
            {
                // Flush

                int chips = 35;

                int cardCount = 0;

                for (int j = 0; j < hand.Length; j++)
                {
                    if (hand[j].PlayingCardSuit == suit)
                    {
                        ++cardCount;

                        chips += GetCardChips(hand[j].PlayingCardRank);

                        if (cardCount == 5)
                            break;
                    }
                }

                Debug.Assert(cardCount == 5);

                if (chips * 4 > bestScore)
                {
                    bestScoreChips = chips;
                    bestScoreMult = 4;
                    bestScore = bestScoreChips * bestScoreMult;
                    bestHand = HandType.Flush;
                }
            }

            if (clubSuitCount >= 5) ScoreFlush(hand, MotelyPlayingCardSuit.Club);
            if (diamondSuitCount >= 5) ScoreFlush(hand, MotelyPlayingCardSuit.Diamond);
            if (heartSuitCount >= 5) ScoreFlush(hand, MotelyPlayingCardSuit.Heart);
            if (spadeSuitCount >= 5) ScoreFlush(hand, MotelyPlayingCardSuit.Spade);

            void SearchForStraightFlush(Span<MotelyItem> hand, MotelyPlayingCardSuit suit)
            {
                for (int i = 0; i < strightStartingCardCount; i++)
                {
                    bool matches = true;

                    for (int j = 0; j < 5; j++)
                    {
                        if (!CardMatches(hand, card => card.PlayingCardRank == straightRankOrder[i + j] && card.PlayingCardSuit == suit))
                        {
                            matches = false;
                            break;
                        }
                    }

                    if (matches)
                    {
                        // Straight flush

                        int chips = 100;

                        for (int j = 0; j < 5; j++)
                        {
                            chips += GetCardChips(straightRankOrder[i + j]);
                        }

                        if (chips * 8 > bestScore)
                        {
                            bestScoreChips = chips;
                            bestScoreMult = 8;
                            bestScore = bestScoreChips * bestScoreMult;
                            bestHand = HandType.StriaghtFlush;
                        }
                    }

                }
            }

            if (hasStraight)
            {
                if (clubSuitCount >= 5) SearchForStraightFlush(hand, MotelyPlayingCardSuit.Club);
                if (diamondSuitCount >= 5) SearchForStraightFlush(hand, MotelyPlayingCardSuit.Diamond);
                if (heartSuitCount >= 5) SearchForStraightFlush(hand, MotelyPlayingCardSuit.Heart);
                if (spadeSuitCount >= 5) SearchForStraightFlush(hand, MotelyPlayingCardSuit.Spade);
            }

            {
                int threeRank = -1;
                int twoRankA = -1;
                int twoRankB = -1;

                for (int i = cardCounts.Length - 1; i >= 0; i--)
                {
                    int cardCount = cardCounts[i];

                    if (cardCount >= 2)
                    {
                        if (twoRankA == -1) twoRankA = i;
                        else if (twoRankB == -1) twoRankB = i;

                        if (cardCount == 3)
                        {
                            if (threeRank == -1) threeRank = i;
                        }
                    }
                }

                if (threeRank != -1 && twoRankA != -1)
                {
                    // Full House
                    int chips = 40 + 3 * GetCardChips((MotelyPlayingCardRank)threeRank) + 2 * GetCardChips((MotelyPlayingCardRank)twoRankA);
                    if (chips * 4 > bestScore)
                    {
                        bestScoreChips = chips;
                        bestScoreMult = 4;
                        bestScore = bestScoreChips * bestScoreMult;
                        bestHand = HandType.FullHouse;
                    }
                }

                if (twoRankA != -1 && twoRankB != -1)
                {
                    // Two Pair
                    int chips = 20 + 2 * GetCardChips((MotelyPlayingCardRank)twoRankA) + 2 * GetCardChips((MotelyPlayingCardRank)twoRankB);
                    if (chips * 2 > bestScore)
                    {
                        bestScoreChips = chips;
                        bestScoreMult = 2;
                        bestScore = bestScoreChips * bestScoreMult;
                        bestHand = HandType.TwoPair;
                    }
                }
            }

            return new(bestHand, bestScoreChips, bestScoreMult);
        }

        private static int GetCardChips(MotelyPlayingCardRank rank)
        {
            return rank switch
            {
                MotelyPlayingCardRank.Ace => 11,
                MotelyPlayingCardRank.King => 10,
                MotelyPlayingCardRank.Queen => 10,
                MotelyPlayingCardRank.Jack => 10,
                MotelyPlayingCardRank.Ten => 10,
                MotelyPlayingCardRank.Nine => 9,
                MotelyPlayingCardRank.Eight => 8,
                MotelyPlayingCardRank.Seven => 7,
                MotelyPlayingCardRank.Six => 6,
                MotelyPlayingCardRank.Five => 5,
                MotelyPlayingCardRank.Four => 4,
                MotelyPlayingCardRank.Three => 3,
                MotelyPlayingCardRank.Two => 2,
                _ => throw new InvalidOperationException(),
            };
        }

        private static bool CardMatches(Span<MotelyItem> hand, Predicate<MotelyItem> predicate)
        {
            foreach (MotelyItem item in hand)
            {
                if (predicate(item)) return true;
            }
            return false;
        }

        /*

        576 23 8
        596 20 14
        

        */

        public VectorMask Filter(ref MotelyVectorSearchContext searchContext)
        {
            return searchContext.SearchIndividualSeeds((ref MotelySingleSearchContext searchContext) =>
            {

                MotelyItem[] deck = new MotelyItem[MotelyEnum<MotelyPlayingCard>.ValueCount];

                for (int i = 0; i < deck.Length; i++)
                {
                    deck[i] = new(MotelyEnum<MotelyPlayingCard>.Values[i]);
                }

                searchContext.Shuffle("nr1", deck);

                // Span<MotelyItem> hand = deck.AsSpan().Slice(deck.Length - 8, 8);
                // double handScore = BestScore(hand).Score;

                // if (handScore < 285 || handScore > 294)
                //     return false;

                // hand = deck.AsSpan().Slice(deck.Length - 16, 8);

                // handScore = BestScore(hand).Score;

                // return handScore == 1208;

                Span<MotelyItem> hand = deck.AsSpan().Slice(deck.Length - 13, 13);

                // int sixCount = 0, fiveCount = 0, sevenCount = 0, threeCount = 0;
                int fiveCount = 0, sevenCount = 0, threeCount = 0;

                foreach (MotelyItem item in hand)
                {
                    switch (item.PlayingCardRank)
                    {
                        case MotelyPlayingCardRank.Seven:
                            ++sevenCount;
                            break;
                        case MotelyPlayingCardRank.Five:
                            ++fiveCount;
                            break;
                        // case MotelyPlayingCardRank.Six:
                        //     ++sixCount;
                        //     break;
                        case MotelyPlayingCardRank.Three:
                            ++threeCount;
                            break;
                    }
                }
                // double handScore = BestScore(hand).Score;

                if (fiveCount < 2 || sevenCount < 2 || threeCount < 2)
                    return false;

                hand = deck.AsSpan().Slice(deck.Length - 21, 8);

                return BestScore(hand).Score == 1208; // Royal flush
            });
        }
    }
}
