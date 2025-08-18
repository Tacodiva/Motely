
namespace Motely;

public enum MotelyItemSeal
{
    None = 0b000 << Motely.ItemSealOffset,
    Gold = 0b001 << Motely.ItemSealOffset,
    Red = 0b010 << Motely.ItemSealOffset,
    Blue = 0b011 << Motely.ItemSealOffset,
    Purple = 0b100 << Motely.ItemSealOffset,
}
