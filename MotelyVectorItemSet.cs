
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Motely;


public ref struct MotelyVectorItemSet
{
    public const int MaxLength = 5;

    [InlineArray(MaxLength)]
    public struct ItemSet
    {
        public MotelyItemVector Item;
    }

    public ItemSet Items;
    public int Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref MotelyItemVector GetItemRef(ref MotelyVectorItemSet set, int index)
    {
#if DEBUG
        return ref set.Items[index];
#else
        // Be fast and skip the bounds check
        return ref Unsafe.Add(ref Unsafe.As<ItemSet, MotelyItemVector>(ref set.Items), index);
#endif
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyItemVector GetItem(int index)
    {
        return GetItemRef(ref this, index);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public void Append(MotelyItemVector item)
    {
        GetItemRef(ref this, Length++) = item;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorMask Contains(MotelyItemType item)
    {
        VectorMask mask = VectorMask.NoBitsSet;

        for (int i = 0; i < Length; i++)
        {
            mask |= VectorEnum256.Equals(GetItemRef(ref this, i).Type, item);
        }

        return mask;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorMask Contains(MotelyItem item)
    {
        VectorMask mask = VectorMask.NoBitsSet;

        for (int i = 0; i < Length; i++)
        {
            mask |= MotelyItemVector.Equals(GetItemRef(ref this, i), item);
        }

        return mask;
    }
}
