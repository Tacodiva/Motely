
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Motely;

public struct PerkeoObservatoryFilterDesc() : IMotelySeedFilterDesc<PerkeoObservatoryFilterDesc.PerkeoObservatoryFilter>
{

    public PerkeoObservatoryFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        ctx.RegisterPseudoRNG("Voucher1");
        ctx.RegisterPseudoHash("Voucher1_resample1");
        ctx.RegisterPseudoHash("Voucher1_resample11");
        return new PerkeoObservatoryFilter();
    }

    public struct PerkeoObservatoryFilter() : IMotelySeedFilter
    {

        public readonly Vector512<double> Filter(ref MotelySearchContext searchContext)
        {
            MotelyVoucherStream stream = searchContext.GetVoucherStream(1);
            MotelyRunStateVoucher voucherState = new();

            VectorEnum256<MotelyVoucher> vouchers = searchContext.GetNextVoucher(ref stream, voucherState);

            Vector256<int> matching = VectorEnum256.Equals(vouchers, MotelyVoucher.Telescope);

            // FancyConsole.WriteLine(vouchers.ToString());

            

            return Vector512.Create(matching, matching).AsDouble();
            // return Vector512<double>.AllBitsSet;
        }
    }
}
