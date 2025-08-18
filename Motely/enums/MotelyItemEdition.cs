
namespace Motely;

public enum MotelyItemEdition
{
    None = 0b000 << Motely.ItemEditionOffset,
    Foil = 0b001 << Motely.ItemEditionOffset,
    Holographic = 0b010 << Motely.ItemEditionOffset,
    Polychrome = 0b011 << Motely.ItemEditionOffset,
    Negative = 0b100 << Motely.ItemEditionOffset,
}