
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Motely;

public static class MotelyVectorUtils
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
    public static Vector256<int> ShiftLeft(Vector256<int> value, Vector256<int> shiftCount)
    {
        return Avx2.ShiftLeftLogicalVariable(value, shiftCount.AsUInt32());
    }
}
