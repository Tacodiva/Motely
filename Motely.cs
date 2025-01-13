
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public class Motely
{
    //RDNCZ58P

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

    public static double PseudoHash(in MotelySeed seed)
        => PseudoHash("", seed);

    public static double PseudoHash(string prefix, in MotelySeed seed)
    {
        int prefixLength = prefix.Length;

        double num = 1;

        for (int i = seed.Length - 1; i >= 0; i--)
        {
            num = ((1.1239285023 / num) * seed.Characters[i] * Math.PI + Math.PI * (i + prefixLength + 1)) % 1;
        }

        for (int i = prefixLength - 1; i >= 0; i--)
        {
            num = ((1.1239285023 / num) * prefix[i] * Math.PI + Math.PI * (i + 1)) % 1;
        }

        return num;
    }

    public static double PseudoRandom(double seed)
    {
        return new LuaRandom(seed).Random();
    }

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

public readonly struct MotelyItem(int value)
{
    public readonly int Value = value;

    public readonly MotelyItemType Type => (MotelyItemType)(Value & Motely.ItemTypeMask);
    public readonly MotelyItemSeal Seal => (MotelyItemSeal)((Value >> Motely.ItemSealOffset) & Motely.ItemSealMask);
    public readonly MotelyItemEnhancement Enhancement => (MotelyItemEnhancement)((Value >> Motely.ItemEnhancementOffset) & Motely.ItemEnhancementMask);
    public readonly MotelyItemEdition Edition => (MotelyItemEdition)((Value >> Motely.ItemEditionOffset) & Motely.ItemEditionMask);

    public MotelyItem AsType(MotelyItemType type)
    {
        return new((Value & ~Motely.ItemTypeMask) | (int)type);
    }

    public MotelyItem WithSeal(MotelyItemSeal seal)
    {
        return new((Value & ~(Motely.ItemSealMask << Motely.ItemSealOffset)) | (int)seal);
    }

    public MotelyItem WithEnhancement(MotelyItemEnhancement enhancement)
    {
        return new((Value & ~(Motely.ItemEnhancementMask << Motely.ItemEnhancementOffset)) | (int)enhancement);
    }

    public MotelyItem WithEdition(MotelyItemEdition edition)
    {
        return new((Value & ~(Motely.ItemEditionMask << Motely.ItemEditionOffset)) | (int)edition);
    }
}

// Card packs



// Base card types:

// spectral, tarot, planets, cards, jokers, packs


// Modifiers:

// editions, enhancements, seals