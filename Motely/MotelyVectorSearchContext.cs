
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public struct MotelyVectorPrngStream(Vector512<double> state)
{
    public static MotelyVectorPrngStream Invalid => new(Vector512.CreateScalar(-1.0));

    public Vector512<double> State = state;
    public readonly bool IsInvalid => State[0] < 0;

    public readonly MotelySinglePrngStream CreateSingleStream(int lane)
    {
        if (IsInvalid)
            return new MotelySinglePrngStream(State[0]);

        return new MotelySinglePrngStream(State[lane]);
    }
}

public struct MotelyVectorResampleStream(MotelyVectorPrngStream initialPrngStream, bool isCached)
{
    public static MotelyVectorResampleStream Invalid => new(MotelyVectorPrngStream.Invalid, false);

    public const int StackResampleCount = 8;

    [InlineArray(StackResampleCount)]
    public struct MotelyResampleStreams
    {
        public MotelyVectorPrngStream PrngStream;
    }

    public MotelyVectorPrngStream InitialPrngStream = initialPrngStream;
    public MotelyResampleStreams ResamplePrngStreams;
    public int ResamplePrngStreamInitCount;
    public List<object>? HighResamplePrngStreams;
    public bool IsCached = isCached;
    public readonly bool IsInvalid => InitialPrngStream.IsInvalid;

    public readonly MotelySingleResampleStream CreateSingleStream(int lane)
    {
        if (IsInvalid)
            return MotelySingleResampleStream.Invalid;

        MotelySingleResampleStream stream = new()
        {
            InitialPrngStream = InitialPrngStream.CreateSingleStream(lane),
            ResamplePrngStreamInitCount = ResamplePrngStreamInitCount,
            IsCached = IsCached
        };

        for (int i = 0; i < ResamplePrngStreamInitCount; i++)
        {
            stream.ResamplePrngStreams[i] = ResamplePrngStreams[i].CreateSingleStream(lane);
        }

        if (HighResamplePrngStreams != null)
        {
            stream.HighResamplePrngStreams = new List<object>(HighResamplePrngStreams.Count);

            for (int i = 0; i < HighResamplePrngStreams.Count; i++)
            {
                stream.HighResamplePrngStreams.Add(
                    Unsafe.Unbox<MotelyVectorPrngStream>(HighResamplePrngStreams[i]).CreateSingleStream(lane)
                );
            }
        }

        return stream;
    }
}

public delegate bool MotelyIndividualSeedSearcher(ref MotelySingleSearchContext searchContext);

internal unsafe readonly struct MotelySearchContextParams(PartialSeedHashCache* seedHashCache, int seedLength, int firstCharactersLength, char* seedFirstCharacters, Vector512<double>* seedLastCharacters, bool isAdditionalFilter = false)
{
    public readonly PartialSeedHashCache* SeedHashCache = seedHashCache;
    public readonly int SeedLength = seedLength;
    public readonly int SeedFirstCharactersLength = firstCharactersLength;
    public readonly int SeedLastCharactersLength => SeedLength - SeedFirstCharactersLength;
    // The first characters which are the same between all vector lanes
    public readonly char* SeedFirstCharacters = seedFirstCharacters;
    // The last characters which are different between vector lanes
    public readonly Vector512<double>* SeedLastCharacters = seedLastCharacters;
    public readonly bool IsAdditionalFilter = isAdditionalFilter;

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly bool IsLaneValid(int lane)
    {
        // If all the lanes are the same seed, we say only the first lane is valid
        if (SeedFirstCharactersLength == SeedLength)
            return lane == 0;

        // Otherwise, the lane is valid if its character is not null
        return ((double*)&SeedLastCharacters[0])[lane] != '\0';
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly string GetSeed(int lane)
    {
        char* seed = stackalloc char[Motely.MaxSeedLength];
        int length = GetSeed(lane, seed);
        return new string(seed, 0, length);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public readonly int GetSeed(int lane, char* output)
    {
        Debug.Assert(IsLaneValid(lane));

        int i = 0;

        for (; i < SeedLastCharactersLength; i++)
        {
            output[i] = (char)((double*)SeedLastCharacters)[i * Vector512<double>.Count + lane];
        }

        for (; i < SeedLength; i++)
        {
            output[i] = SeedFirstCharacters[i - SeedLastCharactersLength];
        }

        return SeedLength;
    }
}

public readonly unsafe ref partial struct MotelyVectorSearchContext
{
    private readonly ref readonly MotelySearchParameters _searchParameters;
    private readonly ref readonly MotelySearchContextParams _contextParams;

    public MotelyStake Stake => _searchParameters.Stake;
    public MotelyDeck Deck => _searchParameters.Deck;

    private PartialSeedHashCache* SeedHashCache => _contextParams.SeedHashCache;
    private int SeedLength => _contextParams.SeedLength;
    private char* SeedFirstCharacters => _contextParams.SeedFirstCharacters;
    private int SeedFirstCharactersLength => _contextParams.SeedFirstCharactersLength;
    private int SeedLastCharactersLength => _contextParams.SeedLastCharactersLength;
    private Vector512<double>* SeedLastCharacters => _contextParams.SeedLastCharacters;
    private bool IsAdditionalFilter => _contextParams.IsAdditionalFilter;

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    internal MotelyVectorSearchContext(ref readonly MotelySearchParameters searchParameters, ref readonly MotelySearchContextParams contextParams)
    {
        _contextParams = ref contextParams;
        _searchParameters = ref searchParameters;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsLaneValid(int lane) => _contextParams.IsLaneValid(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetSeed(int lane) => _contextParams.GetSeed(lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSeed(int lane, char* output) => _contextParams.GetSeed(lane, output);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorMask SearchIndividualSeeds(MotelyIndividualSeedSearcher searcher)
    {
        uint results = 0;

        for (int lane = 0; lane < Vector512<double>.Count; lane++)
        {
            if (IsLaneValid(lane))
            {
                MotelySingleSearchContext singleSearchContext = new(in _searchParameters, in _contextParams, lane);

                bool success = searcher(ref singleSearchContext);

                if (success)
                {
                    results |= 1u << lane;
                }
            }
        }

        return new(results);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorMask SearchIndividualSeeds(VectorMask mask, MotelyIndividualSeedSearcher searcher)
    {
        if (mask.IsAllFalse()) return default;

        uint results = 0;

        uint maskShift = mask.Value;

        for (int lane = 0; lane < Vector512<double>.Count; lane++)
        {
            if ((maskShift & 1) != 0 && IsLaneValid(lane))
            {
                MotelySingleSearchContext singleSearchContext = new(in _searchParameters, in _contextParams, lane);

                bool success = searcher(ref singleSearchContext);

                if (success)
                {
                    results |= 1u << lane;
                }
            }

            maskShift >>= 1;
        }

        return new(results);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> PseudoHash(string key, bool isCached = false)
    {
        Vector512<double> partialHash;

        if ((isCached && !IsAdditionalFilter) || SeedHashCache->HasPartialHash(key.Length))
        {
            partialHash = SeedHashCache->GetPartialHashVector(key.Length);
        }
        else
        {
            partialHash = InternalPseudoHashSeed(key.Length);

            if (key.Length < Motely.MaxCachedPseudoHashKeyLength)
                SeedHashCache->CachePartialHash(key.Length, partialHash);
        }

        return InternalPseudoHash(key, partialHash);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private Vector512<double> InternalPseudoHashSeed(int keyLength)
    {
        int seedLastCharacterLength = SeedLastCharactersLength;
        double num = 1;

        // First we do the first characters of the seed which are the same between all vector lanes
        for (int i = SeedFirstCharactersLength - 1; i >= 0; i--)
        {
            num = (1.1239285023 / num * SeedFirstCharacters[i] * Math.PI + Math.PI * (i + keyLength + seedLastCharacterLength + 1)) % 1;
        }

        // Then we vectorize and do the last characters of the seed
        Vector512<double> numVector = Vector512.Create(num);

        for (int i = seedLastCharacterLength - 1; i >= 0; i--)
        {
            numVector = Vector512.Divide(Vector512.Create(1.1239285023), numVector);

            numVector = Vector512.Multiply(numVector, SeedLastCharacters[i]);

            numVector = Vector512.Multiply(numVector, Math.PI);
            numVector = Vector512.Add(numVector, Vector512.Create((i + keyLength + 1) * Math.PI));

            Vector512<double> intPart = Vector512.Floor(numVector);
            numVector = Vector512.Subtract(numVector, intPart);
        }

        return numVector;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private static Vector512<double> InternalPseudoHash(string key, Vector512<double> partialHash)
    {
        for (int i = key.Length - 1; i >= 0; i--)
        {
            partialHash = Vector512.Divide(Vector512.Create(1.1239285023), partialHash);

            partialHash = Vector512.Multiply(partialHash, key[i]);

            partialHash = Vector512.Multiply(partialHash, Math.PI);
            partialHash = Vector512.Add(partialHash, Vector512.Create((i + 1) * Math.PI));

            Vector512<double> intPart = Vector512.Floor(partialHash);
            partialHash = Vector512.Subtract(partialHash, intPart);
        }

        return partialHash;
    }

// #if !DEBUG
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
// #endif
//     private static void Fract(in Vector512<double> x)
//     {
//         Vector512<ulong> xInt = x.AsUInt64();


//         const ulong DblExpo = 0x7FF0000000000000;
//         const ulong DblMant = 0x000FFFFFFFFFFFFF;

//         const int DblMantSZ = 52;

//         const int DblExpoBias = 1023;

//         Vector512<ulong> expo = (xInt & Vector512.Create(DblExpo)) >> DblMantSZ;

//         Vector512<ulong> edgecaseXMask = Vector512.LessThan(expo, Vector512.Create((ulong)DblExpoBias));

//         Vector512<ulong> expoBiased = expo - Vector512.Create((ulong)DblExpoBias);

//         Vector512<ulong> edgecase0Mask = Vector512.GreaterThan(expoBiased, Vector512.Create((ulong)DblMantSZ));

//         Vector512<ulong> mant = xInt & Vector512.Create(DblMant);
//         Vector512<ulong> fractMant = mant & (
//             MotelyVectorUtils.ShiftLeft(
//                 Vector512.Create(1L),
//                 Vector512.Create(Vector512.Create((ulong)DblMantSZ) - expoBiased).AsInt64()
//             ).AsUInt64() - Vector512<ulong>.One
//         );

//         edgecase0Mask |= Vector512.Equals(fractMant, Vector512.Create(0UL));
        


//         if (expo < DblExpoBias) return x;

//         // const int DblExpoSZ = 11;
//         // if (expo == ((1 << DblExpoSZ) - 1)) return double.NaN;

//         ulong expoBiased = expo - DblExpoBias;

//         if (expoBiased > DblMantSZ) return 0;

//         ulong mant = xInt & DblMant;
//         ulong fractMant = mant & ((1ul << (int)(DblMantSZ - expoBiased)) - 1);

//         if (fractMant == 0) return 0;

//         int fractLzcnt = BitOperations.LeadingZeroCount(fractMant) - (64 - DblMantSZ);
//         ulong resExpo = (expo - (ulong)fractLzcnt - 1) << DblMantSZ;
//         ulong resMant = (fractMant << (fractLzcnt + 1)) & DblMant;

//         ulong res = resExpo | resMant;

//         return Unsafe.As<ulong, double>(ref res);
//     }

    private static readonly double InvPrec = Math.Pow(10.0, 13);
    private static readonly double TwoInvPrec = Math.Pow(2.0, 13);
    private static readonly double FiveInvPrec = Math.Pow(5.0, 13);

    // #if !DEBUG
    //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // #endif
    //     private static double Round13(double x)
    //     {
    //         double normalCase = Math.Round(x * InvPrec, MidpointRounding.AwayFromZero) / InvPrec;

    //         if (normalCase == Math.Round(Math.BitDecrement(x) * InvPrec, MidpointRounding.AwayFromZero) / InvPrec)
    //             return normalCase;

    //         double truncated = Fract(x * TwoInvPrec) * FiveInvPrec;

    //         if (Fract(truncated) >= 0.5)
    //             return (Math.Floor(x * InvPrec) + 1) / InvPrec;

    //         return Math.Floor(x * InvPrec) / InvPrec;
    //     }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private static Vector512<double> IteratePRNG(Vector512<double> state)
    {
        state = Vector512.Multiply(state, 1.72431234);
        state = Vector512.Add(state, Vector512.Create(2.134453429141));

        Vector512<double> intPart = Vector512.Floor(state);
        state = Vector512.Subtract(state, intPart);

        state = Vector512.Multiply(state, 10000000000000);

        state = Vector512.Round(state, MidpointRounding.AwayFromZero);
        state = Vector512.Divide(state, 10000000000000);

        return state;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVectorPrngStream CreatePrngStream(string key, bool isCached = false)
    {
        return new(PseudoHash(key, isCached));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> GetNextPrngState(ref MotelyVectorPrngStream stream)
    {
        return stream.State = IteratePRNG(stream.State);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> GetNextPrngState(ref MotelyVectorPrngStream stream, in Vector512<double> mask)
    {
        return stream.State = Vector512.ConditionalSelect(mask, IteratePRNG(stream.State), stream.State);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePseudoSeed(ref MotelyVectorPrngStream stream)
    {
        return (GetNextPrngState(ref stream) + SeedHashCache->GetSeedHashVector()) / Vector512.Create<double>(2);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> IteratePseudoSeed(ref MotelyVectorPrngStream stream, in Vector512<double> mask)
    {
        return (GetNextPrngState(ref stream, mask) + SeedHashCache->GetSeedHashVector()) / Vector512.Create<double>(2);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> GetNextPseudoSeed(ref MotelyVectorPrngStream stream, in Vector512<double> mask)
    {
        return (GetNextPrngState(ref stream, mask) + SeedHashCache->GetSeedHashVector()) / Vector512.Create<double>(2);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> GetNextRandom(ref MotelyVectorPrngStream stream)
    {
        return VectorLuaRandom.Random(IteratePseudoSeed(ref stream));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector512<double> GetNextRandom(ref MotelyVectorPrngStream stream, in Vector512<double> mask)
    {
        return VectorLuaRandom.Random(GetNextPseudoSeed(ref stream, mask));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector256<int> GetNextRandomInt(ref MotelyVectorPrngStream stream, int min, int max)
    {
        return VectorLuaRandom.RandInt(IteratePseudoSeed(ref stream), min, max);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public Vector256<int> GetNextRandomInt(ref MotelyVectorPrngStream stream, int min, int max, in Vector512<double> mask)
    {
        return VectorLuaRandom.RandInt(IteratePseudoSeed(ref stream, mask), min, max);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorEnum256<T> GetNextRandomElement<T>(ref MotelyVectorPrngStream stream, T[] choices) where T : unmanaged, Enum
    {
        return VectorEnum256.Create(GetNextRandomInt(ref stream, 0, choices.Length), choices);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorEnum256<T> GetNextRandomElement<T>(ref MotelyVectorPrngStream stream, T[] choices, in Vector512<double> mask) where T : unmanaged, Enum
    {
        return VectorEnum256.Create(GetNextRandomInt(ref stream, 0, choices.Length, mask), choices);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyVectorResampleStream CreateResampleStream(string key, bool isCached)
    {
        return new(CreatePrngStream(key, isCached), isCached);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyVectorPrngStream CreateResamplePrngStream(string key, int resample, bool isCached)
    {
        // We don't cache resamples >= 8 because they'd use an extra digit
        if (isCached && resample >= 8) isCached = false;
        return CreatePrngStream(key + MotelyPrngKeys.Resample + (resample + 2), isCached);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private ref MotelyVectorPrngStream GetResamplePrngStream(ref MotelyVectorResampleStream resampleStream, string key, int resample)
    {
        if (resample < MotelyVectorResampleStream.StackResampleCount)
        {
            ref MotelyVectorPrngStream prngStream = ref resampleStream.ResamplePrngStreams[resample];

            if (resample == resampleStream.ResamplePrngStreamInitCount)
            {
                ++resampleStream.ResamplePrngStreamInitCount;

                prngStream = CreateResamplePrngStream(key, resample, resampleStream.IsCached);
            }

            return ref prngStream;
        }

        {
            if (resample == MotelyVectorResampleStream.StackResampleCount)
            {
                resampleStream.HighResamplePrngStreams = [];
            }

            Debug.Assert(resampleStream.HighResamplePrngStreams != null);

            if (resample < resampleStream.HighResamplePrngStreams.Count)
            {
                return ref Unsafe.Unbox<MotelyVectorPrngStream>(resampleStream.HighResamplePrngStreams[resample]);
            }

            object prngStreamObject = new MotelyVectorPrngStream();

            resampleStream.HighResamplePrngStreams.Add(prngStreamObject);

            ref MotelyVectorPrngStream prngStream = ref Unsafe.Unbox<MotelyVectorPrngStream>(prngStreamObject);

            prngStream = CreateResamplePrngStream(key, resample, resampleStream.IsCached);

            return ref prngStream;
        }
    }
}