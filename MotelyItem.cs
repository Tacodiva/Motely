
using System.Runtime.CompilerServices;

namespace Motely;

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