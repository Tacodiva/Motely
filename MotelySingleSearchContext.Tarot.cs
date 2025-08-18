

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Motely;

public ref struct MotelySingleTarotStream(string resampleKey, MotelySingleResampleStream resampleStream, MotelySinglePrngStream soulStream)
{
    public readonly bool IsNull => ResampleKey == null;
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
    private MotelySingleTarotStream CreateTarotStream(string source, int ante, bool searchTarot, bool soulable, bool isCached)
    {
        return new(
            MotelyPrngKeys.Tarot + source + ante,
            searchTarot ?
                CreateResampleStream(MotelyPrngKeys.Tarot + source + ante, isCached) :
                MotelySingleResampleStream.Invalid,
            soulable ?
                CreatePrngStream(MotelyPrngKeys.TerrotSoul + MotelyPrngKeys.Tarot + ante, isCached) :
                MotelySinglePrngStream.Invalid
        );
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleTarotStream CreateArcanaPackTarotStream(int ante, bool soulOnly = false, bool isCached = false) =>
        CreateTarotStream(MotelyPrngKeys.ArcanaPackItemSource, ante, !soulOnly, true, isCached);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleTarotStream CreateShopTarotStream(int ante, bool isCached = false) =>
        CreateTarotStream(MotelyPrngKeys.ShopItemSource, ante, true, false, isCached);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public bool GetNextArcanaPackHasTheSoul(ref MotelySingleTarotStream tarotStream, MotelyBoosterPackSize size)
    {
        Debug.Assert(tarotStream.IsSoulable, "Tarot pack does not have the soul.");
        Debug.Assert(tarotStream.ResampleStream.IsInvalid, "This method is only valid for tarot streams created with soul only.");

        int cardCount = MotelyBoosterPackType.Arcana.GetCardCount(size);

        for (int i = 0; i < cardCount; i++)
        {
            if (GetNextRandom(ref tarotStream.SoulPrngStream) > 0.997)
            {
                return true;
            }
        }

        return false;
    }

    public MotelySingleItemSet GetNextArcanaPackContents(ref MotelySingleTarotStream tarotStream, MotelyBoosterPackSize size)
    {
        int cardCount = MotelyBoosterPackType.Arcana.GetCardCount(size);
        MotelySingleItemSet pack = new();

        for (int i = 0; i < cardCount; i++)
            pack.Append(GetNextTarot(ref tarotStream, pack));

        return pack;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItem GetNextTarot(ref MotelySingleTarotStream tarotStream)
    {
        if (tarotStream.IsSoulable)
        {
            if (GetNextRandom(ref tarotStream.SoulPrngStream) > 0.997)
            {
                return MotelyItemType.Soul;
            }
        }

        if (tarotStream.ResampleStream.IsInvalid)
        {
            return new(MotelyItemType.TarotExcludedByStream);
        }

        return (MotelyItemType)MotelyItemTypeCategory.TarotCard |
            (MotelyItemType)GetNextRandomInt(ref tarotStream.ResampleStream.InitialPrngStream, 0, MotelyEnum<MotelyTarotCard>.ValueCount);
    }

    public MotelyItem GetNextTarot(ref MotelySingleTarotStream tarotStream, in MotelySingleItemSet itemSet)
    {
        if (tarotStream.IsSoulable && !itemSet.Contains(MotelyItemType.Soul))
        {
            if (GetNextRandom(ref tarotStream.SoulPrngStream) > 0.997)
            {
                return MotelyItemType.Soul;
            }
        }

        if (tarotStream.ResampleStream.IsInvalid)
        {
            return new(MotelyItemType.TarotExcludedByStream);
        }

        MotelyItemType tarot = (MotelyItemType)MotelyItemTypeCategory.TarotCard |
            (MotelyItemType)GetNextRandomInt(ref tarotStream.ResampleStream.InitialPrngStream, 0, MotelyEnum<MotelyTarotCard>.ValueCount);
        int resampleCount = 0;

        while (true)
        {
            if (!itemSet.Contains(tarot))
            {
                return tarot;
            }

            tarot = (MotelyItemType)MotelyItemTypeCategory.TarotCard | (MotelyItemType)GetNextRandomInt(
                ref GetResamplePrngStream(ref tarotStream.ResampleStream, tarotStream.ResampleKey, resampleCount),
                0, MotelyEnum<MotelyTarotCard>.ValueCount
            );

            ++resampleCount;
        }
    }
}