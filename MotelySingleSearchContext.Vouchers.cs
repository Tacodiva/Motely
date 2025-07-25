
using System.Runtime.CompilerServices;

namespace Motely;

public ref struct MotelySingleVoucherStream(int ante, MotelySingleResampleStream resampleStream)
{
    public readonly int Ante = ante;
    public MotelySingleResampleStream ResampleStream = resampleStream;
}

ref partial struct MotelySingleSearchContext
{    
#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleVoucherStream CreateVoucherStream(int ante, bool isCached = false)
    {
        return new(ante, CreateResampleStream(MotelyPrngKeys.Voucher + ante, isCached));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVoucher GetAnteFirstVoucher(int ante, bool isCached = false)
    {
        MotelySinglePrngStream prngStream = CreatePrngStream(MotelyPrngKeys.Voucher + ante, isCached);
        MotelyVoucher voucher = (MotelyVoucher)GetNextRandomInt(ref prngStream, 0, MotelyEnum<MotelyVoucher>.ValueCount);
        int resampleCount = 0;

        while (true)
        {
            // All of the odd vouchers require a prerequisite
            bool prerequisiteRequired = ((int)voucher & 1) == 1;

            if (!prerequisiteRequired)
            {
                break;

            }

            prngStream = CreateResamplePrngStream(MotelyPrngKeys.Voucher + ante, resampleCount, isCached);

            voucher = (MotelyVoucher)GetNextRandomInt(
                ref prngStream,
                0, MotelyEnum<MotelyVoucher>.ValueCount
            );

            ++resampleCount;
        }

        return voucher;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVoucher GetAnteFirstVoucher(int ante, in MotelyRunState voucherState, bool isCached = false)
    {
        MotelySinglePrngStream prngStream = CreatePrngStream(MotelyPrngKeys.Voucher + ante, isCached);
        MotelyVoucher voucher = (MotelyVoucher)GetNextRandomInt(ref prngStream, 0, MotelyEnum<MotelyVoucher>.ValueCount);
        int resampleCount = 0;

        while (true)
        {
            if (!voucherState.IsVoucherActive(voucher))
            {
                // All of the odd vouchers require a prerequisite
                bool prerequisiteRequired = ((int)voucher & 1) == 1;

                if (!prerequisiteRequired)
                {
                    break;
                }

                MotelyVoucher prerequisite = voucher - 1;
                bool prerequisiteUnlocked = voucherState.IsVoucherActive(prerequisite);

                if (prerequisiteUnlocked)
                {
                    break;
                }
            }

            prngStream = CreateResamplePrngStream(MotelyPrngKeys.Voucher + ante, resampleCount, isCached);

            voucher = (MotelyVoucher)GetNextRandomInt(
                ref prngStream,
                0, MotelyEnum<MotelyVoucher>.ValueCount
            );

            ++resampleCount;
        }

        return voucher;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVoucher GetNextVoucher(ref MotelySingleVoucherStream voucherStream, in MotelyRunState voucherState)
    {
        MotelyVoucher voucher = (MotelyVoucher)GetNextRandomInt(ref voucherStream.ResampleStream.InitialPrngStream, 0, MotelyEnum<MotelyVoucher>.ValueCount);
        int resampleCount = 0;

        while (true)
        {
            if (!voucherState.IsVoucherActive(voucher))
            {
                // All of the odd vouchers require a prerequisite
                bool prerequisiteRequired = ((int)voucher & 1) == 1;

                if (!prerequisiteRequired)
                {
                    break;
                }

                MotelyVoucher prerequisite = voucher - 1;
                bool prerequisiteUnlocked = voucherState.IsVoucherActive(prerequisite);

                if (prerequisiteUnlocked)
                {
                    break;
                }
            }

            voucher = (MotelyVoucher)GetNextRandomInt(
                ref GetResamplePrngStream(ref voucherStream.ResampleStream, MotelyPrngKeys.Voucher + voucherStream.Ante, resampleCount),
                0, MotelyEnum<MotelyVoucher>.ValueCount
            );

            ++resampleCount;
        }

        return voucher;
    }
}