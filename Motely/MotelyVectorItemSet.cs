
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;

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
    public Vector256<int> Contains(MotelyItemType item)
    {
        Vector256<int> mask = Vector256<int>.Zero;

        for (int i = 0; i < Length; i++)
        {
            mask |= VectorEnum256.Equals<MotelyItemType>(GetItemRef(ref this, i).Type, item);
        }

        return mask;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector256<int> Contains(MotelyItemVector item)
    {
        Vector256<int> mask = Vector256<int>.Zero;

        for (int i = 0; i < Length; i++)
        {
            mask |= MotelyItemVector.Equals(GetItemRef(ref this, i), item);
        }

        return mask;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector256<int> Contains(MotelyItem item)
    {
        Vector256<int> mask = Vector256<int>.Zero;

        for (int i = 0; i < Length; i++)
        {
            mask |= MotelyItemVector.Equals(GetItemRef(ref this, i), item);
        }

        return mask;
    }

    public override string ToString()
    {
        StringBuilder sb = new("[");

        for (int lane = 0; lane < MotelyItemVector.Count; lane++)
        {
            if (lane != 0) sb.Append(", ");
            sb.Append('[');
            for (int i = 0; i < Length; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append(GetItemRef(ref this, i)[lane]);
            }
            sb.Append(']');
        }
        sb.Append(']');
        
        return sb.ToString();
    }
}
