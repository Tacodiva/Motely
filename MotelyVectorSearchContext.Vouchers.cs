
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public ref struct MotelyVectorVoucherStream(int ante, MotelyVectorResampleStream resampleStream)
{
    public readonly int Ante = ante;
    public MotelyVectorResampleStream ResampleStream = resampleStream;
}


ref partial struct MotelyVectorSearchContext
{

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVectorVoucherStream CreateVoucherStreamCached(int ante)
    {
        return new(ante, CreateResampleStreamCached(MotelyPrngKeys.Voucher + ante));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVectorVoucherStream CreateVoucherStream(int ante)
    {
        return new(ante, CreateResampleStream(MotelyPrngKeys.Voucher + ante));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorEnum256<MotelyVoucher> GetAnteFirstVoucher(int ante)
    {
        MotelyVectorPrngStream prngStream = CreatePrngStream(MotelyPrngKeys.Voucher + ante);

        VectorEnum256<MotelyVoucher> vouchers = new(GetNextRandomInt(ref prngStream, 0, MotelyEnum<MotelyVoucher>.ValueCount));
        int resampleCount = 0;

        while (true)
        {
            // All of the odd vouchers require a prerequisite
            Vector256<int> prerequisiteRequiredMask = Vector256.Equals(vouchers.HardwareVector & Vector256<int>.One, Vector256<int>.One);

            // Mask of vouchers we need to resample
            Vector256<int> resampleMask = prerequisiteRequiredMask;

            if (Vector256.EqualsAll(resampleMask, Vector256<int>.Zero))
                break;

            prngStream = CreateResamplePrngStream(MotelyPrngKeys.Voucher + ante, resampleCount);

            Vector256<int> newVouchers = GetNextRandomInt(
                ref prngStream,
                0, MotelyEnum<MotelyVoucher>.ValueCount,
                MotelyVectorUtils.ExtendIntMaskToDouble(resampleMask)
            );

            vouchers = new(Vector256.ConditionalSelect(resampleMask, newVouchers, vouchers.HardwareVector));

            ++resampleCount;
        }

        return vouchers;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorEnum256<MotelyVoucher> GetAnteFirstVoucher(int ante, in MotelyVectorRunStateVoucher voucherState)
    {
        MotelyVectorPrngStream prngStream = CreatePrngStream(MotelyPrngKeys.Voucher + ante);

        VectorEnum256<MotelyVoucher> vouchers = new(GetNextRandomInt(ref prngStream, 0, MotelyEnum<MotelyVoucher>.ValueCount));
        int resampleCount = 0;

        while (true)
        {
            Vector256<int> alreadyUnlockedMask = voucherState.IsVoucherActive(vouchers);

            // All of the odd vouchers require a prerequisite
            Vector256<int> prerequisiteRequiredMask = Vector256.Equals(vouchers.HardwareVector & Vector256<int>.One, Vector256<int>.One);
            VectorEnum256<MotelyVoucher> prerequisiteVouchers = new(vouchers.HardwareVector - Vector256<int>.One);

            Vector256<int> unlockedPrerequisiteMask = voucherState.IsVoucherActive(prerequisiteVouchers);

            Vector256<int> prerequisiteSatisfiedMask = Vector256.ConditionalSelect(prerequisiteRequiredMask, unlockedPrerequisiteMask, Vector256<int>.AllBitsSet);

            // Mask of vouchers we need to resample
            Vector256<int> resampleMask = alreadyUnlockedMask | Vector256.OnesComplement(prerequisiteSatisfiedMask);

            if (Vector256.EqualsAll(resampleMask, Vector256<int>.Zero))
                break;

            prngStream = CreateResamplePrngStream(MotelyPrngKeys.Voucher + ante, resampleCount);

            Vector256<int> newVouchers = GetNextRandomInt(
                ref prngStream,
                0, MotelyEnum<MotelyVoucher>.ValueCount,
                MotelyVectorUtils.ExtendIntMaskToDouble(resampleMask)
            );

            vouchers = new(Vector256.ConditionalSelect(resampleMask, newVouchers, vouchers.HardwareVector));

            ++resampleCount;
        }

        return vouchers;
    }

    public VectorEnum256<MotelyVoucher> GetNextVoucher(ref MotelyVectorVoucherStream voucherStream, in MotelyVectorRunStateVoucher voucherState)
    {
        VectorEnum256<MotelyVoucher> vouchers = new(GetNextRandomInt(ref voucherStream.ResampleStream.InitialPrngStream, 0, MotelyEnum<MotelyVoucher>.ValueCount));
        int resampleCount = 0;

        while (true)
        {
            Vector256<int> alreadyUnlockedMask = voucherState.IsVoucherActive(vouchers);

            // All of the odd vouchers require a prerequisite
            Vector256<int> prerequisiteRequiredMask = Vector256.Equals(vouchers.HardwareVector & Vector256<int>.One, Vector256<int>.One);
            VectorEnum256<MotelyVoucher> prerequisiteVouchers = new(vouchers.HardwareVector - Vector256<int>.One);

            Vector256<int> unlockedPrerequisiteMask = voucherState.IsVoucherActive(prerequisiteVouchers);

            Vector256<int> prerequisiteSatisfiedMask = Vector256.ConditionalSelect(prerequisiteRequiredMask, unlockedPrerequisiteMask, Vector256<int>.AllBitsSet);

            // Mask of vouchers we need to resample
            Vector256<int> resampleMask = alreadyUnlockedMask | Vector256.OnesComplement(prerequisiteSatisfiedMask);

            if (Vector256.EqualsAll(resampleMask, Vector256<int>.Zero))
                break;

            Vector256<int> newVouchers = GetNextRandomInt(
                ref GetResamplePrngStream(ref voucherStream.ResampleStream, MotelyPrngKeys.Voucher + voucherStream.Ante, resampleCount),
                0, MotelyEnum<MotelyVoucher>.ValueCount,
                MotelyVectorUtils.ExtendIntMaskToDouble(resampleMask)
            );

            vouchers = new(Vector256.ConditionalSelect(resampleMask, newVouchers, vouchers.HardwareVector));

            ++resampleCount;
        }

        return vouchers;
    }
}