
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Motely;

public static class MotelyPrngKeys
{
    public const string Resample = "_resample";

    public const string Voucher = "Voucher";
    public const string ShopPack = "shop_pack";

    public const string Tarot = "Tarot";
    public const string TerrotSoul = "soul_";
    public const string ArcanaPackItemSource = "ar1";

    public const string Planet = "Planet";
    public const string PlanetBlackHole = "soul_";
    public const string CelestialPackItemSource = "pl1";

    public const string Spectral = "Spectral";
    public const string SpectralSoulBlackHole = "soul_";
    public const string SpectralPackItemSource = "spe";

    public const string StandardCardBase = "front";
    public const string StandardCardHasEnhancement = "stdset";
    public const string StandardCardEnhancement = "Enhanced";
    public const string StandardCardEdition = "standard_edition";
    public const string StandardCardHasSeal = "stdseal";
    public const string StandardCardSeal = "stdsealtype";
    public const string StandardPackItemSource = "sta";

    public const string BuffoonJokerEternalPerishableSource = "packetper";
    public const string BuffoonJokerRentalSource = "packssjr";
    public const string BuffoonPackItemSource = "buf";

    public const string JokerSoulSource = "sou";
    public const string JokerRarity = "rarity";
    public const string JokerEdition = "edi";
    public const string JokerCommon = "Joker1";
    public const string JokerUncommon = "Joker2";
    public const string JokerRare = "Joker3";
    public const string JokerLegendary = "Joker4";

    public const string Tags = "Tag";
    public const string Boss = "boss";
    
    public const string ShopItemType = "cdt";
    public const string ShopItemSource = "sho";
    public const string DefaultJokerEternalPerishableSource = "etperpoll";
    public const string DefaultJokerRentalSource = "ssjr";

    public const string JokerMisprint = "misprint";
    public const string JokerCavendish = "cavendish";
    public const string JokerGrosMichel = "gros_michel";
    public const string JokerRiffRaff = "rif";

    public const string CardLuckyMoney = "lucky_money";
    public const string CardLuckyMult = "lucky_mult";

    public const string TarotWheelOfFortune = "wheel_of_fortune";
    public const string TarotEmperor = "emp";
    public const string TarotJudgement = "jud";

    public const string TagRare = "rta";
    public const string TagUncommon = "uta";

    public const string SealPurple = "8ba";

    public const string DeckErratic = "erratic";

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static string FixedRarityJoker(MotelyJokerRarity rarity, string source, int ante)
    {
        return rarity switch
        {
            MotelyJokerRarity.Common => JokerCommon + source + ante,
            MotelyJokerRarity.Uncommon => JokerUncommon + source + ante,
            MotelyJokerRarity.Rare => JokerRare + source + ante,
            MotelyJokerRarity.Legendary => JokerLegendary,
            _ => throw new InvalidEnumArgumentException()
        };
    }
}