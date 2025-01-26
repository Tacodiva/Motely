
using System.Runtime.CompilerServices;

namespace Motely;

public class Motely
{
    public const int MaxCachedPseudoHashKeyLength = 20;

    // public static readonly char[] SeedDigits = [.. "CPRDNZ58AB"];
    public static readonly char[] SeedDigits = [.. "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"];
    public const int MaxSeedLength = 8;

    public const int ItemTypeMask = 0xFFFF;

    public const int PlayingCardRankMask = 0b1111;
    public const int PlayingCardSuitMask = 0b11;
    public const int PlayingCardSuitOffset = 4;

    public const int ItemTypeCategoryMask = 0b1111;
    public const int ItemTypeCategoryOffset = 12;

    public const int JokerRarityMask = 0b11;
    public const int JokerRarityOffset = 10;

    public const int ItemSealMask = 0b111;
    public const int ItemSealOffset = 16;

    public const int ItemEnhancementMask = 0b1111;
    public const int ItemEnhancementOffset = 19;

    public const int ItemEditionMask = 0b111;
    public const int ItemEditionOffset = 23;

    public const int BoosterPackTypeOffset = 2;
    public const int BoosterPackSizeMask = 0b11;

}

public enum MotelyTag
{
    UncommonTag,
    RareTag,
    NegativeTag,
    FoilTag,
    HolographicTag,
    PolychromeTag,
    InvestmentTag,
    VoucherTag,
    BossTag,
    StandardTag,
    CharmTag,
    MeteorTag,
    BuffoonTag,
    HandyTag,
    GarbageTag,
    EtherealTag,
    CouponTag,
    DoubleTag,
    JuggleTag,
    D6Tag,
    TopupTag,
    SpeedTag,
    OrbitalTag,
    EconomyTag
}

public enum MotelyItemSeal
{
    None = 0b000 << Motely.ItemSealOffset,
    Gold = 0b001 << Motely.ItemSealOffset,
    Red = 0b010 << Motely.ItemSealOffset,
    Blue = 0b011 << Motely.ItemSealOffset,
    Purple = 0b100 << Motely.ItemSealOffset,
}

public enum MotelyItemEnhancement
{
    None = 0b0000 << Motely.ItemEnhancementOffset,
    Bonus = 0b0001 << Motely.ItemEnhancementOffset,
    Mult = 0b0010 << Motely.ItemEnhancementOffset,
    Wild = 0b0011 << Motely.ItemEnhancementOffset,
    Glass = 0b0100 << Motely.ItemEnhancementOffset,
    Steel = 0b0101 << Motely.ItemEnhancementOffset,
    Stone = 0b0110 << Motely.ItemEnhancementOffset,
    Gold = 0b0111 << Motely.ItemEnhancementOffset,
    Lucky = 0b1000 << Motely.ItemEnhancementOffset,
}

public enum MotelyItemEdition
{
    None = 0b000 << Motely.ItemEditionOffset,
    Foil = 0b001 << Motely.ItemEditionOffset,
    Holographic = 0b010 << Motely.ItemEditionOffset,
    Polychrome = 0b011 << Motely.ItemEditionOffset,
    Negative = 0b100 << Motely.ItemEditionOffset,
}

public enum MotelyItemTypeCategory
{
    PlayingCard = 0b0001 << Motely.ItemTypeCategoryOffset,
    SpectralCard = 0b0010 << Motely.ItemTypeCategoryOffset,
    TarotCard = 0b0011 << Motely.ItemTypeCategoryOffset,
    PlanetCard = 0b0100 << Motely.ItemTypeCategoryOffset,
    Joker = 0b0101 << Motely.ItemTypeCategoryOffset,
}

public enum MotelySpectralCard
{
    Familiar,
    Grim,
    Incantation,
    Talisman,
    Aura,
    Wraith,
    Sigil,
    Ouija,
    Ectoplasm,
    Immolate,
    Ankh,
    DejaVu,
    Hex,
    Trance,
    Medium,
    Cryptid,
    Soul
}

public enum MotelyPlanetCard
{
    Mercury,
    Venus,
    Earth,
    Mars,
    Jupiter,
    Saturn,
    Uranus,
    Neptune,
    Pluto,
    PlanetX,
    Ceres,
    Eris
}

public enum MotelyTarotCard
{
    TheFool,
    TheMagician,
    TheHighPriestess,
    TheEmpress,
    TheEmperor,
    TheHierophant,
    TheLovers,
    TheChariot,
    Justice,
    TheHermit,
    TheWheelOfFortune,
    Strength,
    TheHangedMan,
    Death,
    Temperance,
    TheDevil,
    TheTower,
    TheStar,
    TheMoon,
    TheSun,
    Judgement,
    TheWorld
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct MotelyItem(int value)
{
    public readonly int Value = value;

    public readonly MotelyItemType Type => (MotelyItemType)(Value & Motely.ItemTypeMask);
    public readonly MotelyItemSeal Seal => (MotelyItemSeal)((Value >> Motely.ItemSealOffset) & Motely.ItemSealMask);
    public readonly MotelyItemEnhancement Enhancement => (MotelyItemEnhancement)((Value >> Motely.ItemEnhancementOffset) & Motely.ItemEnhancementMask);
    public readonly MotelyItemEdition Edition => (MotelyItemEdition)((Value >> Motely.ItemEditionOffset) & Motely.ItemEditionMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItem AsType(MotelyItemType type)
    {
        return new((Value & ~Motely.ItemTypeMask) | (int)type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItem WithSeal(MotelyItemSeal seal)
    {
        return new((Value & ~(Motely.ItemSealMask << Motely.ItemSealOffset)) | (int)seal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItem WithEnhancement(MotelyItemEnhancement enhancement)
    {
        return new((Value & ~(Motely.ItemEnhancementMask << Motely.ItemEnhancementOffset)) | (int)enhancement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItem WithEdition(MotelyItemEdition edition)
    {
        return new((Value & ~(Motely.ItemEditionMask << Motely.ItemEditionOffset)) | (int)edition);
    }

    public static implicit operator MotelyItem(MotelyItemType type) => new((int)type);
}

// Card packs



// Base card types:

// spectral, tarot, planets, cards, jokers, packs


// Modifiers:

// editions, enhancements, seals