
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Motely;

public ref struct MotelySingleSpectralStream(string resampleKey, MotelySingleResampleStream resampleStream, MotelySinglePrngStream blackHoleStream)
{
    public readonly bool IsNull => ResampleKey == null;
    public readonly string ResampleKey = resampleKey;
    public MotelySingleResampleStream ResampleStream = resampleStream;
    public MotelySinglePrngStream SoulBlackHolePrngStream = blackHoleStream;
    public readonly bool IsSoulBlackHoleable => !SoulBlackHolePrngStream.IsInvalid;
}

ref partial struct MotelySingleSearchContext
{
#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySingleSpectralStream CreateSpectralStream(string source, int ante, bool searchSpectral, bool soulBlackHoleable, bool isCached)
    {
        return new(
            MotelyPrngKeys.Spectral + source + ante,
            searchSpectral ?
                CreateResampleStream(MotelyPrngKeys.Spectral + source + ante, isCached) :
                MotelySingleResampleStream.Invalid,
            soulBlackHoleable ?
                CreatePrngStream(MotelyPrngKeys.SpectralSoulBlackHole + MotelyPrngKeys.Spectral + ante, isCached) :
                MotelySinglePrngStream.Invalid
        );
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleSpectralStream CreateSpectralPackSpectralStream(int ante, bool soulOnly = false, bool isCached = false) =>
        CreateSpectralStream(MotelyPrngKeys.SpectralPackItemSource, ante, !soulOnly, true, isCached);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleSpectralStream CreateShopSpectralStream(int ante, bool isCached = false) =>
        CreateSpectralStream(MotelyPrngKeys.ShopItemSource, ante, true, false, isCached);


#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public bool GetNextSpectralPackHasTheSoul(ref MotelySingleSpectralStream spectralStream, MotelyBoosterPackSize size)
    {
        Debug.Assert(spectralStream.IsSoulBlackHoleable, "Spectral pack does not have the soul.");

        int cardCount = MotelyBoosterPackType.Spectral.GetCardCount(size);

        // We need to track this so we can keep the prng stream in the right state
        bool hasBlackHole = false;

        for (int i = 0; i < cardCount; i++)
        {
            if (GetNextRandom(ref spectralStream.SoulBlackHolePrngStream) > 0.997)
            {
                // We found the soul!

                // Progress the stream to get ready for the next pack
                for (; i < cardCount && !hasBlackHole; i++)
                {
                    hasBlackHole = GetNextRandom(ref spectralStream.SoulBlackHolePrngStream) > 0.997;
                }
                return true;
            }

            if (!hasBlackHole && GetNextRandom(ref spectralStream.SoulBlackHolePrngStream) > 0.997)
            {
                hasBlackHole = true;
            }
        }

        return false;
    }

    public MotelySingleItemSet GetNextSpectralPackContents(ref MotelySingleSpectralStream spectralStream, MotelyBoosterPackSize size)
        => GetNextSpectralPackContents(ref spectralStream, MotelyBoosterPackType.Spectral.GetCardCount(size));

    public MotelySingleItemSet GetNextSpectralPackContents(ref MotelySingleSpectralStream spectralStream, int size)
    {
        Debug.Assert(size <= MotelySingleItemSet.MaxLength);

        MotelySingleItemSet pack = new();

        for (int i = 0; i < size; i++)
            pack.Append(GetNextSpectral(ref spectralStream, pack));

        return pack;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItem GetNextSpectral(ref MotelySingleSpectralStream spectralStream)
    {
        if (spectralStream.IsSoulBlackHoleable)
        {
            if (GetNextRandom(ref spectralStream.SoulBlackHolePrngStream) > 0.997)
            {
                return MotelyItemType.Soul;
            }

            if (GetNextRandom(ref spectralStream.SoulBlackHolePrngStream) > 0.997)
            {
                return MotelyItemType.BlackHole;
            }
        }

        return (MotelyItemType)MotelyItemTypeCategory.SpectralCard |
            (MotelyItemType)GetNextRandomInt(ref spectralStream.ResampleStream.InitialPrngStream, 0, MotelyEnum<MotelySpectralCard>.ValueCount);
    }

    public MotelyItem GetNextSpectral(ref MotelySingleSpectralStream SpectralStream, in MotelySingleItemSet itemSet)
    {
        if (SpectralStream.IsSoulBlackHoleable)
        {
            if (!itemSet.Contains(MotelyItemType.Soul) && GetNextRandom(ref SpectralStream.SoulBlackHolePrngStream) > 0.997)
            {
                return MotelyItemType.Soul;
            }

            if (!itemSet.Contains(MotelyItemType.BlackHole) && GetNextRandom(ref SpectralStream.SoulBlackHolePrngStream) > 0.997)
            {
                return MotelyItemType.BlackHole;
            }
        }

        MotelyItemType Spectral = (MotelyItemType)MotelyItemTypeCategory.SpectralCard |
            (MotelyItemType)GetNextRandomInt(ref SpectralStream.ResampleStream.InitialPrngStream, 0, MotelyEnum<MotelySpectralCard>.ValueCount);

        int resampleCount = 0;

        while (true)
        {
            if (!itemSet.Contains(Spectral))
            {
                return Spectral;
            }

            Spectral = (MotelyItemType)MotelyItemTypeCategory.SpectralCard | (MotelyItemType)GetNextRandomInt(
                ref GetResamplePrngStream(ref SpectralStream.ResampleStream, SpectralStream.ResampleKey, resampleCount),
                0, MotelyEnum<MotelyVoucher>.ValueCount
            );

            ++resampleCount;
        }
    }
}