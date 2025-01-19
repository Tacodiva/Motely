using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;


public ref struct MotelyRunStateVoucher
{
    static MotelyRunStateVoucher()
    {
        // Check that we can fit all the voucher state in an int
        if (MotelyEnum<MotelyVoucher>.ValueCount > 32)
            throw new UnreachableException();
    }

    public Vector256<int> StateBitfield;

    public void ActivateVoucher(MotelyVoucher voucher)
    {
        StateBitfield |= Vector256.Create(1 << (int)voucher);
    }

    public void ActivateVoucher(VectorEnum256<MotelyVoucher> voucherVector)
    {
        StateBitfield |= MotelyVectorUtils.ShiftLeft(Vector256<int>.One, voucherVector.HardwareVector);
    }

    public Vector256<int> IsVoucherActive(MotelyVoucher voucher)
    {
        return Vector256.OnesComplement(Vector256.IsZero(
            StateBitfield & Vector256.Create(1 << (int)voucher)
        ));

    }

    public Vector256<int> IsVoucherActive(VectorEnum256<MotelyVoucher> voucherVector)
    {
        return Vector256.OnesComplement(Vector256.IsZero(
            StateBitfield & MotelyVectorUtils.ShiftLeft(Vector256<int>.One, voucherVector.HardwareVector)
        ));
    }
}


public ref struct MotelyVoucherStream(int ante, MotelyPrngStream prngStream)
{
    public const int StackResampleCount = 8;

    [InlineArray(StackResampleCount)]
    public struct MotelyVoucherResampleStreams
    {
        public MotelyPrngStream PrngStream;
    }

    public readonly int Ante = ante;
    public MotelyPrngStream MainStream = prngStream;
    public MotelyVoucherResampleStreams ResampleStreams;
    public int ResampleStreamInitCount;
    public List<object>? HighResampleStreams;
    

}
