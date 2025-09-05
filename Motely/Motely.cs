namespace Motely;

public class Motely
{
    public const int MaxCachedPseudoHashKeyLength = 32;

    public static readonly char[] SeedDigits = [.. "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"];
    public const int MaxSeedLength = 8;

    public const int MaxVectorWidth = 8; // Equals Vector512<double>.Count (but a const)

    public const int ItemTypeMask = 0xFFFF;

    public const int PlayingCardRankMask = 0b1111;
    public const int PlayingCardSuitOffset = 4;
    public const int PlayingCardSuitMask = 0b11 << PlayingCardSuitOffset;

    public const int ItemTypeCategoryOffset = 12;
    public const int ItemTypeCategoryMask = 0b1111 << ItemTypeCategoryOffset;

    public const int JokerRarityOffset = 10;
    public const int JokerRarityMask = 0b11 << JokerRarityOffset;

    public const int ItemSealOffset = 16;
    public const int ItemSealMask = 0b111 << ItemSealOffset;

    public const int ItemEnhancementOffset = 19;
    public const int ItemEnhancementMask = 0b1111 << ItemEnhancementOffset;

    public const int ItemEditionOffset = 23;
    public const int ItemEditionMask = 0b111 << ItemEditionOffset;

    public const int BoosterPackTypeOffset = 2;
    public const int BoosterPackTypeMask = 0b11 << BoosterPackTypeOffset;
    public const int BoosterPackSizeMask = 0b11;

    public const int PerishableStickerOffset = 31;
    public const int EternalStickerOffset = 30;
    public const int RentalStickerOffset = 29;

    public const int BossTypeOffset = 31;
    public const int BossTypeMask = 0b1 << BossTypeOffset;
    public const int BossRequiredAnteOffset = 28;
    public const int BossRequiredAnteMask = 0b111 << BossRequiredAnteOffset;


}
