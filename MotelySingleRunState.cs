
using System.Diagnostics;

namespace Motely;


public ref struct MotelySingleRunStateVoucher
{
    static MotelySingleRunStateVoucher()
    {
        // Check that we can fit all the voucher state in an int
        if (MotelyEnum<MotelyVoucher>.ValueCount > 32)
            throw new UnreachableException();
    }

    public int StateBitfield;

    public void ActivateVoucher(MotelyVoucher voucher)
    {
        StateBitfield |= 1 << (int)voucher;
    }

    public bool IsVoucherActive(MotelyVoucher voucher)
    {
        return (StateBitfield & (1 << (int) voucher)) != 0;
    }
}
