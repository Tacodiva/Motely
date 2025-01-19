
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

[method:MethodImpl(MethodImplOptions.AggressiveInlining)]
public struct VectorMask(uint mask)
{
    public const int Length = 8;

    public static readonly VectorMask AllBitsSet = new(0xFF);

    public uint Value = mask;

    public bool this[int i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (Value & (1 << i)) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (value)
            {
                Value |= 1u << i;
            }
            else
            {
                Value &= ~(1u << i);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsAllTrue() => Value == 0xFF;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsAllFalse() => Value == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsPartiallyTrue() => Value != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VectorMask operator &(VectorMask a, VectorMask b) => new(a.Value & b.Value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VectorMask operator |(VectorMask a, VectorMask b) => new(a.Value | b.Value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VectorMask operator ^(VectorMask a, VectorMask b) => new(a.Value ^ b.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VectorMask(Vector256<int> vec) => new(MotelyVectorUtils.VectorMaskToIntMask(vec));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VectorMask(Vector256<uint> vec) => new(MotelyVectorUtils.VectorMaskToIntMask(vec));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VectorMask(Vector256<float> vec) => new(MotelyVectorUtils.VectorMaskToIntMask(vec));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VectorMask(Vector512<long> vec) => new(MotelyVectorUtils.VectorMaskToIntMask(vec));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VectorMask(Vector512<ulong> vec) => new(MotelyVectorUtils.VectorMaskToIntMask(vec));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VectorMask(Vector512<double> vec) => new(MotelyVectorUtils.VectorMaskToIntMask(vec));
}