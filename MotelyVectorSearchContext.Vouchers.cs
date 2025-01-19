
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public ref struct MotelyVectorVoucherStream(int ante, MotelyVectorPrngStream prngStream)
{
    public const int StackResampleCount = 8;

    [InlineArray(StackResampleCount)]
    public struct MotelyVoucherResampleStreams
    {
        public MotelyVectorPrngStream PrngStream;
    }

    public readonly int Ante = ante;
    public MotelyVectorPrngStream MainStream = prngStream;
    public MotelyVoucherResampleStreams ResampleStreams;
    public int ResampleStreamInitCount;
    public List<object>? HighResampleStreams;
}


ref partial struct MotelyVectorSearchContext
{

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyVectorPrngStream CreateVoucherPrngStream(int ante)
    {
        return CreatePrngStream(MotelyPrngKeys.Voucher + ante);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyVectorPrngStream CreateVoucherResamplePrngStream(int ante, int resample)
    {
        return CreatePrngStream(MotelyPrngKeys.Voucher + ante + MotelyPrngKeys.Resample + (resample + 2));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVectorVoucherStream CreateVoucherStream(int ante)
    {
        return new(ante, CreateVoucherPrngStream(ante));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorEnum256<MotelyVoucher> GetAnteFirstVoucher(int ante)
    {
        MotelyVectorPrngStream prngStream = CreateVoucherPrngStream(ante);

        VectorEnum256<MotelyVoucher> vouchers = new(IteratePrngRandomInt(ref prngStream, 0, MotelyEnum<MotelyVoucher>.ValueCount));
        int resampleCount = 0;

        while (true)
        {
            // All of the odd vouchers require a prerequisite
            Vector256<int> prerequisiteRequiredMask = Vector256.Equals(vouchers.HardwareVector & Vector256<int>.One, Vector256<int>.One);

            // Mask of vouchers we need to resample
            Vector256<int> resampleMask = prerequisiteRequiredMask;

            if (Vector256.EqualsAll(resampleMask, Vector256<int>.Zero))
                break;

            prngStream = CreateVoucherResamplePrngStream(ante, resampleCount);

            Vector256<int> newVouchers = IteratePrngRandomInt(
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
        MotelyVectorPrngStream prngStream = CreateVoucherPrngStream(ante);

        VectorEnum256<MotelyVoucher> vouchers = new(IteratePrngRandomInt(ref prngStream, 0, MotelyEnum<MotelyVoucher>.ValueCount));
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

            prngStream = CreateVoucherResamplePrngStream(ante, resampleCount);

            Vector256<int> newVouchers = IteratePrngRandomInt(
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
    private ref MotelyVectorPrngStream GetVoucherStreamResamplePrngStream(ref MotelyVectorVoucherStream voucherStream, int resample)
    {

        if (resample < MotelyVectorVoucherStream.StackResampleCount)
        {
            ref MotelyVectorPrngStream prngStream = ref voucherStream.ResampleStreams[resample];

            if (resample == voucherStream.ResampleStreamInitCount)
            {
                ++voucherStream.ResampleStreamInitCount;
                prngStream = CreateVoucherResamplePrngStream(voucherStream.Ante, resample);
            }

            return ref prngStream;
        }

        {
            if (resample == MotelyVectorVoucherStream.StackResampleCount)
            {
                voucherStream.HighResampleStreams = [];
            }

            Debug.Assert(voucherStream.HighResampleStreams != null);

            if (resample < voucherStream.HighResampleStreams.Count)
            {
                return ref Unsafe.Unbox<MotelyVectorPrngStream>(voucherStream.HighResampleStreams[resample]);
            }

            object prngStreamObject = new MotelyVectorPrngStream();

            voucherStream.HighResampleStreams.Add(prngStreamObject);

            ref MotelyVectorPrngStream prngStream = ref Unsafe.Unbox<MotelyVectorPrngStream>(prngStreamObject);

            prngStream = CreateVoucherResamplePrngStream(voucherStream.Ante, resample);

            return ref prngStream;
        }
    }

    public VectorEnum256<MotelyVoucher> GetNextVoucher(ref MotelyVectorVoucherStream voucherStream, in MotelyVectorRunStateVoucher voucherState)
    {
        VectorEnum256<MotelyVoucher> vouchers = new(IteratePrngRandomInt(ref voucherStream.MainStream, 0, MotelyEnum<MotelyVoucher>.ValueCount));
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

            Vector256<int> newVouchers = IteratePrngRandomInt(
                ref GetVoucherStreamResamplePrngStream(ref voucherStream, resampleCount),
                0, MotelyEnum<MotelyVoucher>.ValueCount,
                MotelyVectorUtils.ExtendIntMaskToDouble(resampleMask)
            );

            vouchers = new(Vector256.ConditionalSelect(resampleMask, newVouchers, vouchers.HardwareVector));

            ++resampleCount;
        }

        return vouchers;
    }
}