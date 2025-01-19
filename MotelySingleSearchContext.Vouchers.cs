
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Motely;

public ref struct MotelySingleVoucherStream(int ante, MotelySinglePrngStream prngStream)
{
    public const int StackResampleCount = 16;

    [InlineArray(StackResampleCount)]
    public struct MotelyVoucherResampleStreams
    {
        public MotelySinglePrngStream PrngStream;
    }

    public readonly int Ante = ante;
    public MotelySinglePrngStream MainStream = prngStream;
    public MotelyVoucherResampleStreams ResampleStreams;
    public int ResampleStreamInitCount;
    public List<object>? HighResampleStreams;
}

ref partial struct MotelySingleSearchContext
{
#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySinglePrngStream CreateVoucherPrngStream(int ante)
    {
        return CreatePrngStream(MotelyPrngKeys.Voucher + ante);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySinglePrngStream CreateVoucherResamplePrngStream(int ante, int resample)
    {
        return CreatePrngStream(MotelyPrngKeys.Voucher + ante + MotelyPrngKeys.Resample + (resample + 2));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleVoucherStream CreateVoucherStream(int ante)
    {
        return new(ante, CreateVoucherPrngStream(ante));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVoucher GetAnteFirstVoucher(int ante)
    {
        MotelySinglePrngStream prngStream = CreateVoucherPrngStream(ante);
        MotelyVoucher voucher = (MotelyVoucher)IteratePrngRandomInt(ref prngStream, 0, MotelyEnum<MotelyVoucher>.ValueCount);
        int resampleCount = 0;

        while (true)
        {
            // All of the odd vouchers require a prerequisite
            bool prerequisiteRequired = ((int)voucher & 1) == 1;

            if (!prerequisiteRequired)
            {
                break;

            }

            prngStream = CreateVoucherResamplePrngStream(ante, resampleCount);

            voucher = (MotelyVoucher)IteratePrngRandomInt(
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
    public MotelyVoucher GetAnteFirstVoucher(int ante, in MotelySingleRunStateVoucher voucherState)
    {
        MotelySinglePrngStream prngStream = CreateVoucherPrngStream(ante);
        MotelyVoucher voucher = (MotelyVoucher)IteratePrngRandomInt(ref prngStream, 0, MotelyEnum<MotelyVoucher>.ValueCount);
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

            prngStream = CreateVoucherResamplePrngStream(ante, resampleCount);

            voucher = (MotelyVoucher)IteratePrngRandomInt(
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
    private ref MotelySinglePrngStream GetVoucherStreamResamplePrngStream(ref MotelySingleVoucherStream voucherStream, int resample)
    {

        if (resample < MotelySingleVoucherStream.StackResampleCount)
        {
            ref MotelySinglePrngStream prngStream = ref voucherStream.ResampleStreams[resample];

            if (resample == voucherStream.ResampleStreamInitCount)
            {
                ++voucherStream.ResampleStreamInitCount;
                prngStream = CreateVoucherResamplePrngStream(voucherStream.Ante, resample);
            }

            return ref prngStream;
        }

        {
            if (resample == MotelySingleVoucherStream.StackResampleCount)
            {
                voucherStream.HighResampleStreams = [];
            }

            Debug.Assert(voucherStream.HighResampleStreams != null);

            if (resample < voucherStream.HighResampleStreams.Count)
            {
                return ref Unsafe.Unbox<MotelySinglePrngStream>(voucherStream.HighResampleStreams[resample]);
            }

            object prngStreamObject = new MotelySinglePrngStream();

            voucherStream.HighResampleStreams.Add(prngStreamObject);

            ref MotelySinglePrngStream prngStream = ref Unsafe.Unbox<MotelySinglePrngStream>(prngStreamObject);

            prngStream = CreateVoucherResamplePrngStream(voucherStream.Ante, resample);

            return ref prngStream;
        }
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVoucher GetNextVoucher(ref MotelySingleVoucherStream voucherStream, in MotelySingleRunStateVoucher voucherState)
    {
        MotelyVoucher voucher = (MotelyVoucher)IteratePrngRandomInt(ref voucherStream.MainStream, 0, MotelyEnum<MotelyVoucher>.ValueCount);
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

            voucher = (MotelyVoucher)IteratePrngRandomInt(
                ref GetVoucherStreamResamplePrngStream(ref voucherStream, resampleCount),
                0, MotelyEnum<MotelyVoucher>.ValueCount
            );

            ++resampleCount;
        }

        return voucher;
    }
}