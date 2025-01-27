
namespace Motely;

public enum MotelyItemTypeCategory
{
    PlayingCard = 0b0001 << Motely.ItemTypeCategoryOffset,
    SpectralCard = 0b0010 << Motely.ItemTypeCategoryOffset,
    TarotCard = 0b0011 << Motely.ItemTypeCategoryOffset,
    PlanetCard = 0b0100 << Motely.ItemTypeCategoryOffset,
    Joker = 0b0101 << Motely.ItemTypeCategoryOffset,
}
