

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Motely;

public ref struct MotelySingleTarotStream(string resampleKey, MotelySingleResampleStream resampleStream, MotelySinglePrngStream soulStream)
{
    public readonly string ResampleKey = resampleKey;
    public MotelySingleResampleStream ResampleStream = resampleStream;
    public MotelySinglePrngStream SoulPrngStream = soulStream;
    public readonly bool IsSoulable => !SoulPrngStream.IsInvalid;
}

ref partial struct MotelySingleSearchContext
{
#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySingleTarotStream CreateTarotStream(string source, int ante, bool soulable)
    {
        return new(
            MotelyPrngKeys.Tarot + source + ante,
            CreateResampleStream(MotelyPrngKeys.Tarot + source + ante),
            soulable ?
                CreatePrngStream(MotelyPrngKeys.TerrotSoul + MotelyPrngKeys.Tarot + ante) :
                MotelySinglePrngStream.Invalid
        );
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySingleTarotStream CreateTarotStreamCached(string source, int ante, bool soulable)
    {
        return new(
            MotelyPrngKeys.Tarot + source + ante,
            CreateResampleStreamCached(MotelyPrngKeys.Tarot + source + ante),
            soulable ?
                CreatePrngStreamCached(MotelyPrngKeys.TerrotSoul + MotelyPrngKeys.Tarot + ante) :
                MotelySinglePrngStream.Invalid
        );
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleTarotStream CreateArcanaPackTarotStreamCached(int ante) =>
        CreateTarotStreamCached(MotelyPrngKeys.ArcanaPackItemSource, ante, true);


#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleTarotStream CreateArcanaPackTarotStream(int ante) =>
        CreateTarotStream(MotelyPrngKeys.ArcanaPackItemSource, ante, true);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleTarotStream CreateShopTarotStreamCached(int ante) =>
        CreateTarotStreamCached(MotelyPrngKeys.ShopItemSource, ante, false);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleTarotStream CreateShopTarotStream(int ante) =>
        CreateTarotStream(MotelyPrngKeys.ShopItemSource, ante, false);


    public MotelySingleItemSet GetArcanaPackContents(ref MotelySingleTarotStream tarotStream, MotelyBoosterPackSize size)
        => GetArcanaPackContents(ref tarotStream, size switch
        {
            MotelyBoosterPackSize.Normal => 3,
            MotelyBoosterPackSize.Jumbo => 5,
            MotelyBoosterPackSize.Mega => 5,
            _ => throw new InvalidEnumArgumentException()
        });

    public MotelySingleItemSet GetArcanaPackContents(ref MotelySingleTarotStream tarotStream, int size)
    {
        Debug.Assert(size <= MotelySingleItemSet.MaxLength);

        MotelySingleItemSet pack = new();

        for (int i = 0; i < size; i++)
            pack.Append(GetNextTarot(ref tarotStream, pack));

        return pack;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItem GetNextTarot(ref MotelySingleTarotStream tarotStream)
        => GetNextTarot(ref tarotStream, MotelySingleItemSet.Empty);

    public MotelyItem GetNextTarot(ref MotelySingleTarotStream tarotStream, in MotelySingleItemSet itemSet)
    {
        if (!tarotStream.IsSoulable && !itemSet.Contains(MotelyItemType.Soul))
        {
            if (GetNextRandom(ref tarotStream.SoulPrngStream) > 0.997)
            {
                return MotelyItemType.Soul;
            }
        }

        MotelyItemType tarot = (MotelyItemType)MotelyItemTypeCategory.TarotCard | (MotelyItemType)GetNextRandomInt(ref tarotStream.ResampleStream.InitialPrngStream, 0, MotelyEnum<MotelyTarotCard>.ValueCount);
        int resampleCount = 0;

        while (true)
        {
            if (!itemSet.Contains(tarot))
            {
                return tarot;
            }

            tarot = (MotelyItemType)MotelyItemTypeCategory.TarotCard | (MotelyItemType)GetNextRandomInt(
                ref GetResamplePrngStream(ref tarotStream.ResampleStream, tarotStream.ResampleKey, resampleCount),
                0, MotelyEnum<MotelyVoucher>.ValueCount
            );

            ++resampleCount;
        }
    }
}