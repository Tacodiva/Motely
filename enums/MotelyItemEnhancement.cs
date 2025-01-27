
namespace Motely;

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