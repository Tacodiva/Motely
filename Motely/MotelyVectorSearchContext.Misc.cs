
using System.Runtime.Intrinsics;

namespace Motely;


ref partial struct MotelyVectorSearchContext
{

    #region Misprint

    public MotelyVectorPrngStream CreateMisprintPrngStream(bool isCached = false)
        => CreatePrngStream(MotelyPrngKeys.JokerMisprint, isCached);

    public Vector256<int> GetNextMisprintMult(ref MotelyVectorPrngStream misprintStream)
        => GetNextRandomInt(ref misprintStream, Motely.JokerMisprintMin, Motely.JokerMisprintMax + 1);
    #endregion

    #region Lucky Cards

    public MotelyVectorPrngStream CreateLuckyCardMoneyStream(bool isCached = false)
        => CreatePrngStream(MotelyPrngKeys.CardLuckyMoney, isCached);

    public VectorMask GetNextLuckyMoney(ref MotelyVectorPrngStream moneyStream, double baseLuck = 1)
        => Vector512.LessThan(GetNextRandom(ref moneyStream), Vector512.Create(baseLuck / Motely.EnhancementLuckyMoneyChance));


    public MotelyVectorPrngStream CreateLuckyCardMultStream(bool isCached = false)
        => CreatePrngStream(MotelyPrngKeys.CardLuckyMult, isCached);

    public VectorMask GetNextLuckyMult(ref MotelyVectorPrngStream multStream, double baseLuck = 1)
        => Vector512.LessThan(GetNextRandom(ref multStream), Vector512.Create(baseLuck / Motely.EnhancementLuckyMultChance));

    #endregion

    #region Wheel of Fortune
    public MotelyVectorPrngStream CreateWheelOfFortuneStream(bool isCached = false)
        => CreatePrngStream(MotelyPrngKeys.TarotWheelOfFortune, isCached);

    public VectorEnum256<MotelyItemEdition> GetNextWheelOfFortune(ref MotelyVectorPrngStream wheelStream, double baseLuck = 1)
    {

        Vector512<double> successMask = Vector512.LessThan(GetNextRandom(ref wheelStream), Vector512.Create(baseLuck / Motely.TarrotWheelChance));

        // The game picks which joker to apply the effect to, but we don't implement that
        GetNextPrngState(ref wheelStream, successMask);

        Vector512<double> editionPoll = GetNextRandom(ref wheelStream, successMask);

        return new(Vector256.ConditionalSelect(
            MotelyVectorUtils.ShrinkDoubleMaskToInt(successMask),
            Vector256.ConditionalSelect(
                MotelyVectorUtils.ShrinkDoubleMaskToInt(Vector512.GreaterThan(editionPoll, Vector512.Create(1 - 0.006 * 25))),
                Vector256.Create((int)MotelyItemEdition.Polychrome),
                Vector256.ConditionalSelect(
                    MotelyVectorUtils.ShrinkDoubleMaskToInt(Vector512.GreaterThan(editionPoll, Vector512.Create(1 - 0.02 * 25))),
                    Vector256.Create((int)MotelyItemEdition.Holographic),
                    Vector256.Create((int)MotelyItemEdition.Foil)
                )
            ),
            Vector256.Create((int)MotelyItemEdition.None)
        ));
    }

    #endregion

    #region Banannanas
    public MotelyVectorPrngStream CreateCavendishPrngStream(bool isCached)
        => CreatePrngStream(MotelyPrngKeys.JokerCavendish, isCached);

    public VectorMask GetNextCavendishExtinct(ref MotelyVectorPrngStream cavendishStream, double baseLuck = 1)
        => Vector512.LessThan(GetNextRandom(ref cavendishStream), Vector512.Create(baseLuck / Motely.JokerCavendishChance));

    public MotelyVectorPrngStream CreateGrosMichelPrngStream(bool isCached)
        => CreatePrngStream(MotelyPrngKeys.JokerGrosMichel, isCached);

    public VectorMask GetNextGrosMichelExtinct(ref MotelyVectorPrngStream grosMichelStream, double baseLuck = 1)
        => Vector512.LessThan(GetNextRandom(ref grosMichelStream), Vector512.Create(baseLuck / Motely.JokerGrosMichelChance));

    #endregion

    #region Erratic

    public MotelyVectorPrngStream CreateErraticDeckPrngStream(bool isCached = false)
        => CreatePrngStream(MotelyPrngKeys.DeckErratic, isCached);

    public MotelyItemVector GetNextErraticDeckCard(ref MotelyVectorPrngStream erraticDeckStream)
        => new(Vector256.BitwiseOr(
            GetNextRandomElement(ref erraticDeckStream, MotelyEnum<MotelyPlayingCard>.Values).HardwareVector,
            Vector256.Create((int)MotelyItemTypeCategory.PlayingCard)
        ));

    #endregion
}