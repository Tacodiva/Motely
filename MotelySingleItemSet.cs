
using System.Runtime.CompilerServices;
using System.Text;

namespace Motely;

public ref struct MotelySingleItemSet
{
    public static MotelySingleItemSet Empty => default;

    public const int MaxLength = 5;

    [InlineArray(MaxLength)]
    public struct ItemSet
    {
        public MotelyItem Item;
    }

    public ItemSet Items;
    public int Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref MotelyItem GetItemRef(ref MotelySingleItemSet set, int index)
    {
#if DEBUG
        return ref set.Items[index];
#else
        // Be fast and skip the bounds check
        return ref Unsafe.Add(ref Unsafe.As<ItemSet, MotelyItem>(ref set.Items), index);
#endif
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyItem GetItem(int index)
    {
        return GetItemRef(ref this, index);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public void Append(MotelyItem item)
    {
        GetItemRef(ref this, Length++) = item;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public bool Contains(MotelyItemType item)
    {
        for (int i = 0; i < Length; i++)
        {
            if (GetItemRef(ref this, i).Type == item)
            {
                return true;
            }
        }

        return false;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public bool Contains(MotelyItem item)
    {
        for (int i = 0; i < Length; i++)
        {
            if (GetItemRef(ref this, i) == item)
            {
                return true;
            }
        }

        return false;
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        sb.Append('[');

        for (int i = 0; i < Length; i++)
        {
            if (i != 0) sb.Append(", ");
            sb.Append(GetItemRef(ref this, i).ToString());
        }

        sb.Append(']');

        return sb.ToString();
    }
}
