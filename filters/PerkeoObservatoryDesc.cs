
using System.Diagnostics;
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

        public readonly VectorMask Filter(ref MotelyVectorSearchContext searchContext)
        {
            VectorEnum256<MotelyVoucher> vouchers = searchContext.GetAnteFirstVoucher(1);

            VectorMask matching = VectorEnum256.Equals(vouchers, MotelyVoucher.Telescope);

            if (matching.IsAllFalse())
                return Vector512<double>.Zero;

            MotelyVectorRunStateVoucher voucherState = new();
            voucherState.ActivateVoucher(MotelyVoucher.Telescope);

            vouchers = searchContext.GetAnteFirstVoucher(2, voucherState);

            matching &= VectorEnum256.Equals(vouchers, MotelyVoucher.Observatory);

            return searchContext.SearchIndividualSeeds(matching, (ref MotelySingleSearchContext searchContext) => {

                bool matching = searchContext.GetAnteFirstVoucher(1) == MotelyVoucher.Telescope;

                if (!matching)
                    throw new UnreachableException();

                MotelySingleRunStateVoucher voucherState = new();
                voucherState.ActivateVoucher(MotelyVoucher.Telescope);

                if (searchContext.GetAnteFirstVoucher(2, voucherState) != MotelyVoucher.Observatory)
                    throw new UnreachableException();

                return true;
            });
        }
    }
}
