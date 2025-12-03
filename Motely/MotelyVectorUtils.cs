using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace Motely;

public unsafe static class MotelyVectorUtils
{
    public static bool IsAccelerated => Vector512.IsHardwareAccelerated;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<int> ConvertToVector256Int32(in Vector512<double> vector)
    {
        if (Avx512F.IsSupported)
        {
            return Avx512F.ConvertToVector256Int32WithTruncation(vector);
        }
        else
        {
            Vector512<long> integerVector = Vector512.ConvertToInt64(vector);
            return Vector256.Narrow(integerVector.GetLower(), integerVector.GetUpper());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<int> ShiftLeft(in Vector256<int> value, in Vector256<int> shiftCount)
    {
        if (AdvSimd.IsSupported)
        {
            return Vector256.Create(
                AdvSimd.ShiftLogical(value.GetLower(), shiftCount.GetLower()),
                AdvSimd.ShiftLogical(value.GetUpper(), shiftCount.GetUpper())
            );
        }

        if (Avx2.IsSupported)
        {
            return Avx2.ShiftLeftLogicalVariable(value, shiftCount.AsUInt32());
        }

        int* temp = stackalloc int[Vector256<int>.Count];

        temp[0] = value[0] & shiftCount[0];
        temp[1] = value[1] & shiftCount[1];
        temp[2] = value[2] & shiftCount[2];
        temp[3] = value[3] & shiftCount[3];
        temp[4] = value[4] & shiftCount[4];
        temp[5] = value[5] & shiftCount[5];
        temp[6] = value[6] & shiftCount[6];
        temp[7] = value[7] & shiftCount[7];

        return *(Vector256<int>*)temp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector512<long> ShiftLeft(in Vector512<long> value, in Vector512<long> shiftCount)
    {
        if (AdvSimd.IsSupported)
        {
            return Vector512.Create(
                Vector256.Create(
                    AdvSimd.ShiftLogical(value.GetLower().GetLower(), shiftCount.GetLower().GetLower()),
                    AdvSimd.ShiftLogical(value.GetLower().GetUpper(), shiftCount.GetLower().GetUpper())
                ),
                Vector256.Create(
                    AdvSimd.ShiftLogical(value.GetUpper().GetLower(), shiftCount.GetUpper().GetLower()),
                    AdvSimd.ShiftLogical(value.GetUpper().GetUpper(), shiftCount.GetUpper().GetUpper())
                )
            );
        }

        if (Avx512F.IsSupported)
        {
            return Avx512F.ShiftLeftLogicalVariable(value, shiftCount.AsUInt64());
        }

        if (Avx2.IsSupported)
        {
            return Vector512.Create(
                Avx2.ShiftLeftLogicalVariable(value.GetLower(), shiftCount.GetLower().AsUInt64()),
                Avx2.ShiftLeftLogicalVariable(value.GetUpper(), shiftCount.GetUpper().AsUInt64())
            );
        }

        long* temp = stackalloc long[Vector512<long>.Count];

        temp[0] = value[0] & shiftCount[0];
        temp[1] = value[1] & shiftCount[1];
        temp[2] = value[2] & shiftCount[2];
        temp[3] = value[3] & shiftCount[3];
        temp[4] = value[4] & shiftCount[4];
        temp[5] = value[5] & shiftCount[5];
        temp[6] = value[6] & shiftCount[6];
        temp[7] = value[7] & shiftCount[7];

        return *(Vector512<long>*)temp;
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

        (Vector256<long> low, Vector256<long> high) = Vector256.Widen(smallMask.AsInt32());
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

        return Vector256.Narrow(smallMask.GetLower().AsUInt64(), smallMask.GetUpper().AsUInt64()).As<uint, TTo>();
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