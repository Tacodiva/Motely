
namespace Motely;


ref partial struct MotelySingleSearchContext
{

    #region Misprint

    public MotelySinglePrngStream CreateMisprintPrngStream(bool isCached = false)
        => CreatePrngStream(MotelyPrngKeys.JokerMisprint, isCached);

    public int GetNextMisprintMult(ref MotelySinglePrngStream misprintStream)
        => GetNextRandomInt(ref misprintStream, Motely.JokerMisprintMin, Motely.JokerMisprintMax + 1);
    #endregion

    #region Lucky Cards

    public MotelySinglePrngStream CreateLuckyCardMoneyStream(bool isCached = false)
        => CreatePrngStream(MotelyPrngKeys.CardLuckyMoney, isCached);

    public bool GetNextLuckyMoney(ref MotelySinglePrngStream moneyStream, double baseLuck = 1)
        => GetNextRandom(ref moneyStream) < baseLuck / Motely.EnhancementLuckyMoneyChance;


    public MotelySinglePrngStream CreateLuckyCardMultStream(bool isCached = false)
        => CreatePrngStream(MotelyPrngKeys.CardLuckyMult, isCached);

    public bool GetNextLuckyMult(ref MotelySinglePrngStream multStream, double baseLuck = 1)
        => GetNextRandom(ref multStream) < baseLuck / Motely.EnhancementLuckyMultChance;

    #endregion

    #region Wheel of Fortune
    public MotelySinglePrngStream CreateWheelOfFortuneStream(bool isCached = false)
        => CreatePrngStream(MotelyPrngKeys.TarotWheelOfFortune, isCached);

    public MotelyItemEdition GetNextWheelOfFortune(ref MotelySinglePrngStream wheelStream, double baseLuck = 1)
    {
        if (GetNextRandom(ref wheelStream) >= baseLuck / Motely.TarrotWheelChance)
            return MotelyItemEdition.None;

        // The game picks which joker to apply the effect to, but we don't implement that
        GetNextPrngState(ref wheelStream);

        double editionPoll = GetNextRandom(ref wheelStream);

        if (editionPoll > 1 - 0.006 * 25)
            return MotelyItemEdition.Polychrome;

        if (editionPoll > 1 - 0.02 * 25)
            return MotelyItemEdition.Holographic;

        return MotelyItemEdition.Foil;
    }

    #endregion

    #region Banannanas
    public MotelySinglePrngStream CreateCavendishPrngStream(bool isCached)
        => CreatePrngStream(MotelyPrngKeys.JokerCavendish, isCached);

    public bool GetNextCavendishExtinct(ref MotelySinglePrngStream cavendishStream, double baseLuck = 1)
        => GetNextRandom(ref cavendishStream) < baseLuck / Motely.JokerCavendishChance;

    public MotelySinglePrngStream CreateGrosMichelPrngStream(bool isCached)
        => CreatePrngStream(MotelyPrngKeys.JokerGrosMichel, isCached);

    public bool GetNextGrosMichelExtinct(ref MotelySinglePrngStream grosMichelStream, double baseLuck = 1)
        => GetNextRandom(ref grosMichelStream) < baseLuck / Motely.JokerGrosMichelChance;

    #endregion

}