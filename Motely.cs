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
