
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public struct MotelySinglePrngStream(double state)
{
    public static MotelySinglePrngStream Invalid => new(-1);

    public double State = state;
    public readonly bool IsInvalid => State < 0;
}

public struct MotelySingleResampleStream(MotelySinglePrngStream initialPrngStream, bool isCached)
{

    public static MotelySingleResampleStream Invalid => new(MotelySinglePrngStream.Invalid, false);

    public const int StackResampleCount = 4;

    [InlineArray(StackResampleCount)]
    public struct MotelyResampleStreams
    {
        public MotelySinglePrngStream PrngStream;
    }

    public MotelySinglePrngStream InitialPrngStream = initialPrngStream;
    public MotelyResampleStreams ResamplePrngStreams;
    public int ResamplePrngStreamInitCount;
    public List<object>? HighResamplePrngStreams;
    public bool IsCached = isCached;

    public readonly bool IsInvalid => InitialPrngStream.IsInvalid;
}


public readonly unsafe ref partial struct MotelySingleSearchContext
{
    public readonly int VectorLane;

    private readonly ref readonly MotelySearchParameters _searchParameters;
    private readonly ref readonly MotelySearchContextParams _contextParams;

    public MotelyStake Stake => _searchParameters.Stake;
    public MotelyDeck Deck => _searchParameters.Deck;

    private PartialSeedHashCache* SeedHashCache => _contextParams.SeedHashCache;
    private int SeedLength => _contextParams.SeedLength;
    private int SeedFirstCharactersLength => _contextParams.SeedFirstCharactersLength;
    private int SeedLastCharactersLength => _contextParams.SeedLastCharactersLength;
    private char* SeedFirstCharacters => _contextParams.SeedFirstCharacters;
    private Vector512<double>* SeedLastCharacters => _contextParams.SeedLastCharacters;
    private bool IsAdditionalFilter => _contextParams.IsAdditionalFilter;

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    internal MotelySingleSearchContext(ref readonly MotelySearchParameters searchParameters, ref readonly MotelySearchContextParams contextParams, int lane)
    {
        _contextParams = ref contextParams;
        _searchParameters = ref searchParameters;
        VectorLane = lane;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetSeed() => _contextParams.GetSeed(VectorLane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSeed(char* output) => _contextParams.GetSeed(VectorLane, output);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public double PseudoHash(string key, bool isCached = false)
    {
        double partialHash;

        if ((isCached && !IsAdditionalFilter) || SeedHashCache->HasPartialHash(key.Length))
        {
            partialHash = SeedHashCache->GetPartialHash(key.Length, VectorLane);
        }
        else
        {
            partialHash = InternalPseudoHashSeed(key.Length);
        }

        return InternalPseudoHashKey(key, partialHash);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private double InternalPseudoHashKey(string key, double partialHash)
    {
        double num = partialHash;

        for (int i = key.Length - 1; i >= 0; i--)
        {
            num = (1.1239285023 / num * key[i] * Math.PI + (i + 1) * Math.PI) % 1;
        }

        return num;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private double InternalPseudoHashSeed(int keyLength)
    {
        int seedLastCharacterLength = SeedLastCharactersLength;
        double num = 1;

        // First we do the first characters of the seed which are the same between all vector lanes
        for (int i = SeedFirstCharactersLength - 1; i >= 0; i--)
        {
            num = (1.1239285023 / num * SeedFirstCharacters[i] * Math.PI + Math.PI * (i + keyLength + seedLastCharacterLength + 1)) % 1;
        }

        // Then we get the characters for our lane
        for (int i = seedLastCharacterLength - 1; i >= 0; i--)
        {
            num = (1.1239285023 / num * SeedLastCharacters[i][VectorLane] * Math.PI + Math.PI * (keyLength + i + 1)) % 1;
        }

        return num;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private static double Fract(double x)
    {
        ref ulong xInt = ref Unsafe.As<double, ulong>(ref x);

        const ulong DblExpo = 0x7FF0000000000000;
        const ulong DblMant = 0x000FFFFFFFFFFFFF;

        const int DblMantSZ = 52;

        const int DblExpoBias = 1023;

        ulong expo = (xInt & DblExpo) >> DblMantSZ;

        if (expo < DblExpoBias) return x;

        // We don't have to worry about this edge case
        
        // const int DblExpoSZ = 11;
        // if (expo == ((1 << DblExpoSZ) - 1)) return double.NaN;

        ulong expoBiased = expo - DblExpoBias;

        if (expoBiased > DblMantSZ) return 0;

        ulong mant = xInt & DblMant;
        ulong fractMant = mant & ((1ul << (int)(DblMantSZ - expoBiased)) - 1);

        if (fractMant == 0) return 0;

        int fractLzcnt = BitOperations.LeadingZeroCount(fractMant) - (64 - DblMantSZ);
        ulong resExpo = (expo - (ulong)fractLzcnt - 1) << DblMantSZ;
        ulong resMant = (fractMant << (fractLzcnt + 1)) & DblMant;

        ulong res = resExpo | resMant;

        return Unsafe.As<ulong, double>(ref res);
    }

    private static readonly double InvPrec = Math.Pow(10.0, 13);
    private static readonly double TwoInvPrec = Math.Pow(2.0, 13);
    private static readonly double FiveInvPrec = Math.Pow(5.0, 13);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private static double Round13(double x)
    {
        double normalCase = Math.Round(x * InvPrec, MidpointRounding.AwayFromZero) / InvPrec;

        if (normalCase == Math.Round(Math.BitDecrement(x) * InvPrec, MidpointRounding.AwayFromZero) / InvPrec)
            return normalCase;

        double truncated = Fract(x * TwoInvPrec) * FiveInvPrec;

        if (Fract(truncated) >= 0.5)
            return (Math.Floor(x * InvPrec) + 1) / InvPrec;

        return Math.Floor(x * InvPrec) / InvPrec;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private static double IteratePRNG(double state)
    {
        return Round13(Fract(state * 1.72431234 + 2.134453429141));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySinglePrngStream CreatePrngStream(string key, bool isCached = false)
    {
        return new(PseudoHash(key, isCached));
    }

#if DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public double GetNextPrngState(ref MotelySinglePrngStream stream)
    {
        Debug.Assert(!stream.IsInvalid, "Invalid stream.");
        return stream.State = IteratePRNG(stream.State);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public double GetNextPseudoSeed(ref MotelySinglePrngStream stream)
    {
        return (GetNextPrngState(ref stream) + SeedHashCache->GetSeedHash(VectorLane)) / 2d;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public LuaRandom GetNextLuaRandom(ref MotelySinglePrngStream stream)
    {
        return new LuaRandom(GetNextPseudoSeed(ref stream));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public double GetNextRandom(ref MotelySinglePrngStream stream)
    {
        return LuaRandom.Random(GetNextPseudoSeed(ref stream));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public int GetNextRandomInt(ref MotelySinglePrngStream stream, int min, int max)
    {
        return LuaRandom.RandInt(GetNextPseudoSeed(ref stream), min, max);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public T GetNextRandomElement<T>(ref MotelySinglePrngStream stream, T[] choices)
    {
        return choices[GetNextRandomInt(ref stream, 0, choices.Length)];
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySingleResampleStream CreateResampleStream(string key, bool isCached)
    {
        return new(CreatePrngStream(key, isCached), isCached);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySinglePrngStream CreateResamplePrngStream(string key, int resample, bool isCached)
    {
        // We don't cache resamples >= 8 because they'd use an extra digit
        if (isCached && resample >= 8) isCached = false;
        return CreatePrngStream(key + MotelyPrngKeys.Resample + (resample + 2), isCached);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private ref MotelySinglePrngStream GetResamplePrngStream(ref MotelySingleResampleStream resampleStream, string key, int resample)
    {
        if (resample < MotelySingleResampleStream.StackResampleCount)
        {
            ref MotelySinglePrngStream prngStream = ref resampleStream.ResamplePrngStreams[resample];

            if (resample == resampleStream.ResamplePrngStreamInitCount)
            {
                ++resampleStream.ResamplePrngStreamInitCount;
                prngStream = CreateResamplePrngStream(key, resample, resampleStream.IsCached);
            }

            return ref prngStream;
        }
        else
        {
            if (resample == MotelySingleResampleStream.StackResampleCount)
            {
                resampleStream.HighResamplePrngStreams = [];
            }

            Debug.Assert(resampleStream.HighResamplePrngStreams != null);

            if (resample < resampleStream.HighResamplePrngStreams.Count)
            {
                return ref Unsafe.Unbox<MotelySinglePrngStream>(resampleStream.HighResamplePrngStreams[resample]);
            }

            object prngStreamObject = new MotelySinglePrngStream();

            resampleStream.HighResamplePrngStreams.Add(prngStreamObject);

            ref MotelySinglePrngStream prngStream = ref Unsafe.Unbox<MotelySinglePrngStream>(prngStreamObject);
            prngStream = CreateResamplePrngStream(key, resample, resampleStream.IsCached);

            return ref prngStream;
        }
    }

}