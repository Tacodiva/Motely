
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

        public readonly Vector512<double> Filter(ref MotelyVectorSearchContext searchContext)
        {
            VectorEnum256<MotelyVoucher> vouchers = searchContext.GetAnteFirstVoucher(1);

            Vector256<int> matching = VectorEnum256.Equals(vouchers, MotelyVoucher.Telescope);

            if (Vector256.EqualsAll(matching, Vector256<int>.Zero))
                return Vector512<double>.Zero;

            MotelyVectorRunStateVoucher voucherState = new();
            voucherState.ActivateVoucher(MotelyVoucher.Telescope);

            vouchers = searchContext.GetAnteFirstVoucher(2, voucherState);

            matching &= VectorEnum256.Equals(vouchers, MotelyVoucher.Observatory);

            return MotelyVectorUtils.ExtendIntMaskToDouble(matching);
        }
    }
}
