
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Motely;

public unsafe static class MotelyVectorUtils
{
    public static readonly bool IsAccelerated;

    static MotelyVectorUtils()
    {
        IsAccelerated = Avx512F.IsSupported && Avx2.IsSupported;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<int> ConvertToVector256Int32(in Vector512<double> vector)
    {
        return Avx512F.ConvertToVector256Int32WithTruncation(vector);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<int> ShiftLeft(in Vector256<int> value, in Vector256<int> shiftCount)
    {
        return Avx2.ShiftLeftLogicalVariable(value, shiftCount.AsUInt32());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector512<double> ExtendFloatMaskToDouble(in Vector256<float> smallMask)
        => Extend32MaskTo64<float, double>(smallMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector512<long> ExtendIntMaskToLong(in Vector256<int> smallMask)
        => Extend32MaskTo64<int, long>(smallMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector512<long> ExtendFloatMaskToLong(in Vector256<int> smallMask)
        => Extend32MaskTo64<int, long>(smallMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector512<double> ExtendIntMaskToDouble(in Vector256<int> smallMask)
        => Extend32MaskTo64<int, double>(smallMask);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static Vector512<TTo> Extend32MaskTo64<TFrom, TTo>(in Vector256<TFrom> smallMask)
        where TFrom : unmanaged
        where TTo : unmanaged
    {
        if (sizeof(TFrom) != 4) throw new InvalidOperationException();
        if (sizeof(TTo) != 8) throw new InvalidOperationException();

        Vector256<long> low = Avx2.ConvertToVector256Int64(smallMask.AsInt32().GetLower());
        Vector256<long> high = Avx2.ConvertToVector256Int64(smallMask.AsInt32().GetUpper());

        return Vector512.Create(low, high).As<long, TTo>();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> ShrinkDoubleMaskToFloat(in Vector512<double> smallMask)
        => Shrink64MaskTo32<double, float>(smallMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> ShrinkLongMaskToFloat(in Vector512<long> smallMask)
        => Shrink64MaskTo32<long, float>(smallMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<int> ShrinkDoubleMaskToInt(in Vector512<double> smallMask)
        => Shrink64MaskTo32<double, int>(smallMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<int> ShrinkLongMaskToInt(in Vector512<long> smallMask)
        => Shrink64MaskTo32<long, int>(smallMask);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static Vector256<TTo> Shrink64MaskTo32<TFrom, TTo>(in Vector512<TFrom> smallMask)
        where TFrom : unmanaged
        where TTo : unmanaged
    {
        if (sizeof(TTo) != 4) throw new InvalidOperationException();
        if (sizeof(TFrom) != 8) throw new InvalidOperationException();

        return Avx512F.ConvertToVector256Int32(smallMask.AsUInt64()).As<int, TTo>();
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static uint VectorMaskToIntMask<T>(in Vector256<T> vector) where T : unmanaged
    {
        if (sizeof(T) != 4) throw new InvalidOperationException();
        return Vector256.ExtractMostSignificantBits(vector);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static uint VectorMaskToIntMask<T>(in Vector512<T> vector) where T : unmanaged
    {
        if (sizeof(T) != 8) throw new InvalidOperationException();
        return (uint)Vector512.ExtractMostSignificantBits(vector);
    }
}
