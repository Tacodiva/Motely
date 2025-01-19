
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public unsafe ref struct MotelySearchContext(Vector512<double>* seedHashes, int* seedHashesLookup)
{

    private readonly int* _seedHashesLookup = seedHashesLookup;
    // A map of pseudohash key length => seed hash up to that point
    private readonly Vector512<double>* _seedHashes = seedHashes;

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> PseudoHash(string key)
    {
#if MOTELY_SAFE
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(key.Length, Motely.MaxPseudoHashKeyLength);
#endif
        Vector512<double> calcVector = _seedHashes[_seedHashesLookup[key.Length]];

        for (int i = key.Length - 1; i >= 0; i--)
        {
            calcVector = Vector512.Divide(Vector512.Create(1.1239285023), calcVector);

            calcVector = Vector512.Multiply(calcVector, key[i]);

            calcVector = Vector512.Multiply(calcVector, Math.PI);
            calcVector = Vector512.Add(calcVector, Vector512.Create((i + 1) * Math.PI));

            Vector512<double> intPart = Vector512.Floor(calcVector);
            calcVector = Vector512.Subtract(calcVector, intPart);
        }

        return calcVector;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static Vector512<double> IteratePRNG(Vector512<double> state)
    {
        state = Vector512.Multiply(state, 1.72431234);
        state = Vector512.Add(state, Vector512.Create(2.134453429141));

        Vector512<double> intPart = Vector512.Floor(state);
        state = Vector512.Subtract(state, intPart);

        state = Vector512.Multiply(state, 10000000000000);

        state = Vector512.Round(state);
        state = Vector512.Divide(state, 10000000000000);

        return state;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyPrngStream GetPrngStream(string key)
    {
        return new(PseudoHash(key));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public void IteratePrngStream(ref MotelyPrngStream stream)
    {
        stream.State = IteratePRNG(stream.State);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public void IteratePrngStream(ref MotelyPrngStream stream, in Vector512<double> mask)
    {
        stream.State = Vector512.ConditionalSelect(mask, IteratePRNG(stream.State), stream.State);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePseudoSeed(ref MotelyPrngStream stream)
    {
        IteratePrngStream(ref stream);
        return (stream.State + _seedHashes[0]) / Vector512.Create<double>(2);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePseudoSeed(ref MotelyPrngStream stream, in Vector512<double> mask)
    {
        IteratePrngStream(ref stream, mask);
        return (stream.State + _seedHashes[0]) / Vector512.Create<double>(2);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePrngRandom(ref MotelyPrngStream stream)
    {
        return VectorLuaRandomSingle.Random(IteratePseudoSeed(ref stream));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePrngRandom(ref MotelyPrngStream stream, in Vector512<double> mask)
    {
        return VectorLuaRandomSingle.Random(IteratePseudoSeed(ref stream, mask));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector256<int> IteratePrngRandomInt(ref MotelyPrngStream stream, int min, int max)
    {
        Vector512<double> seed = IteratePseudoSeed(ref stream);
        return VectorLuaRandomSingle.RandInt(seed, min, max);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector256<int> IteratePrngRandomInt(ref MotelyPrngStream stream, int min, int max, in Vector512<double> mask)
    {
        return VectorLuaRandomSingle.RandInt(IteratePseudoSeed(ref stream), min, max);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorEnum256<T> IteratePrngRandElement<T>(ref MotelyPrngStream stream, T[] choices) where T : unmanaged, Enum
    {
        return VectorEnum256.Create(IteratePrngRandomInt(ref stream, 0, choices.Length), choices);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorEnum256<T> IteratePrngRandElement<T>(ref MotelyPrngStream stream, T[] choices, in Vector512<double> mask) where T : unmanaged, Enum
    {
        return VectorEnum256.Create(IteratePrngRandomInt(ref stream, 0, choices.Length, mask), choices);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVoucherStream GetVoucherStream(int ante)
    {
        return new(ante, GetPrngStream("Voucher" + ante));
    }

    private ref MotelyPrngStream GetVoucherResamplePrngStream(ref MotelyVoucherStream voucherStream, int resample)
    {

        if (resample < MotelyVoucherStream.StackResampleCount)
        {
            ref MotelyPrngStream prngStream = ref voucherStream.ResampleStreams[resample];

            if (resample == voucherStream.ResampleStreamInitCount)
            {
                ++voucherStream.ResampleStreamInitCount;


                prngStream = GetPrngStream("Voucher" + voucherStream.Ante + "_resample" + (resample + 2));
            }

            return ref prngStream;
        }

        throw new NotSupportedException();
        {
            // if (resample == MotelyVoucherStream.StackResampleCount)
            // {
            //     voucherStream.HighResampleStreams = [];
            // }

            // Debug.Assert(voucherStream.HighResampleStreams != null);

            // if (resample < voucherStream.HighResampleStreams.Count)
            // {
            //     return ref Unsafe.Unbox<MotelyPrngStream>(voucherStream.HighResampleStreams[resample]);
            // }

            // object prngStreamObject = new MotelyPrngStream();

            // voucherStream.HighResampleStreams.Add(prngStreamObject);

            // ref MotelyPrngStream prngStream = ref Unsafe.Unbox<MotelyPrngStream>(prngStreamObject);

            // prngStream = GetPrngStream("Voucher" + voucherStream.Ante + "_resample" + (resample + 2));

            // return ref prngStream;
        }
    }

    public VectorEnum256<MotelyVoucher> GetNextVoucher(ref MotelyVoucherStream voucherStream, in MotelyRunStateVoucher voucherState)
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
                ref GetVoucherResamplePrngStream(ref voucherStream, resampleCount),
                0, MotelyEnum<MotelyVoucher>.ValueCount,
                Vector512.Create(resampleMask, resampleMask).AsDouble()
            );

            vouchers = new(Vector256.ConditionalSelect(resampleMask, newVouchers, vouchers.HardwareVector));

            ++resampleCount;
        }


        return vouchers;
    }
}