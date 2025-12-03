
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public struct MotelyVectorTarotStream(string resampleKey, MotelyVectorResampleStream resampleStream, MotelyVectorPrngStream soulStream)
{
    public readonly bool IsNull => ResampleKey == null;
    public readonly string ResampleKey = resampleKey;
    public MotelyVectorResampleStream ResampleStream = resampleStream;
    public MotelyVectorPrngStream SoulPrngStream = soulStream;
    public readonly bool IsSoulable => !SoulPrngStream.IsInvalid;

    public readonly MotelySingleTarotStream CreateSingleStream(int lane)
    {
        return new(
            ResampleKey, ResampleStream.CreateSingleStream(lane), SoulPrngStream.CreateSingleStream(lane)
        );
    }
}

ref partial struct MotelyVectorSearchContext
{
#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyVectorTarotStream CreateTarotStream(string source, int ante, bool searchTarot, bool soulable, bool isCached)
    {
        return new(
            MotelyPrngKeys.Tarot + source + ante,
            searchTarot ?
                CreateResampleStream(MotelyPrngKeys.Tarot + source + ante, isCached) :
                MotelyVectorResampleStream.Invalid,
            soulable ?
                CreatePrngStream(MotelyPrngKeys.TerrotSoul + MotelyPrngKeys.Tarot + ante, isCached) :
                MotelyVectorPrngStream.Invalid
        );
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVectorTarotStream CreateArcanaPackTarotStream(int ante, bool soulOnly = false, bool isCached = false) =>
        CreateTarotStream(MotelyPrngKeys.ArcanaPackItemSource, ante, !soulOnly, true, isCached);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVectorTarotStream CreateShopTarotStream(int ante, bool isCached = false) =>
        CreateTarotStream(MotelyPrngKeys.ShopItemSource, ante, true, false, isCached);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVectorTarotStream CreateEmperorTarotStream(int ante, bool isCached = false) =>
        CreateTarotStream(MotelyPrngKeys.TarotEmperor, ante, true, false, isCached);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVectorTarotStream CreatePurpleSealTarotStream(int ante, bool isCached = false) =>
        CreateTarotStream(MotelyPrngKeys.SealPurple, ante, true, false, isCached);

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorMask GetNextArcanaPackHasTheSoul(ref MotelyVectorTarotStream tarotStream, MotelyBoosterPackSize size)
    {
        Debug.Assert(tarotStream.IsSoulable, "Tarot pack does not have the soul.");
        Debug.Assert(tarotStream.ResampleStream.IsInvalid, "This method is only valid for tarot streams created with soul only.");

        int cardCount = MotelyBoosterPackType.Arcana.GetCardCount(size);

        Vector512<double> hasSoulMask = Vector512<double>.Zero;

        for (int i = 0; i < cardCount; i++)
        {
            hasSoulMask |= Vector512.GreaterThan(GetNextRandom(ref tarotStream.SoulPrngStream, ~hasSoulMask), Vector512.Create(0.997));
        }

        return hasSoulMask;
    }

    public MotelyVectorItemSet GetNextArcanaPackContents(ref MotelyVectorTarotStream tarotStream, MotelyBoosterPackSize size)
    {
        int cardCount = MotelyBoosterPackType.Arcana.GetCardCount(size);
        MotelyVectorItemSet pack = new();

        for (int i = 0; i < cardCount; i++)
            pack.Append(GetNextTarot(ref tarotStream, pack));

        return pack;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVectorItemSet GetNextEmperorTarots(ref MotelyVectorTarotStream tarotStream)
    {
        Debug.Assert(!tarotStream.IsSoulable, "Emperor tarot stream should not have the soul.");

        MotelyVectorItemSet items = new();

        items.Append(GetNextTarot(ref tarotStream));
        items.Append(GetNextTarot(ref tarotStream, items));

        return items;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItemVector GetNextTarot(ref MotelyVectorTarotStream tarotStream)
    {
        Vector512<double> soulMask;

        if (tarotStream.IsSoulable)
        {
            soulMask = Vector512.GreaterThan(GetNextRandom(ref tarotStream.SoulPrngStream), Vector512.Create(0.997));
        }
        else
        {
            soulMask = Vector512<double>.Zero;
        }

        Vector256<int> tarots;

        if (tarotStream.ResampleStream.IsInvalid)
        {
            tarots = Vector256.Create(new MotelyItem(MotelyItemType.TarotExcludedByStream).Value);
        }
        else
        {
            tarots = GetNextRandomInt(
                ref tarotStream.ResampleStream.InitialPrngStream,
                0, MotelyEnum<MotelyTarotCard>.ValueCount, ~soulMask
            );

            tarots = Vector256.Create((int)MotelyItemTypeCategory.TarotCard) | tarots;
        }

        if (!tarotStream.IsSoulable)
        {
            return new(tarots);
        }

        return new(Vector256.ConditionalSelect(
            MotelyVectorUtils.ShrinkDoubleMaskToInt(soulMask),
            Vector256.Create(new MotelyItem(MotelyItemType.Soul).Value),
            tarots
        ));
    }

    public MotelyItemVector GetNextTarot(ref MotelyVectorTarotStream tarotStream, in MotelyVectorItemSet itemSet)
    {

        Vector512<double> soulMaskDbl;
        Vector256<int> soulMaskInt;

        if (tarotStream.IsSoulable)
        {
            Vector512<double> soulValidMask = MotelyVectorUtils.ExtendIntMaskToDouble(~itemSet.Contains(MotelyItemType.Soul));
            soulMaskDbl = soulValidMask & Vector512.GreaterThan(GetNextRandom(ref tarotStream.SoulPrngStream, soulValidMask), Vector512.Create(0.997));
            soulMaskInt = MotelyVectorUtils.ShrinkDoubleMaskToInt(soulMaskDbl);
        }
        else
        {
            soulMaskDbl = Vector512<double>.Zero;
            soulMaskInt = Vector256<int>.Zero;
        }


        Vector256<int> tarots;

        if (tarotStream.ResampleStream.IsInvalid)
        {
            tarots = Vector256.Create(new MotelyItem(MotelyItemType.TarotExcludedByStream).Value);
        }
        else
        {
            tarots = GetNextRandomInt(
                ref tarotStream.ResampleStream.InitialPrngStream,
                0, MotelyEnum<MotelyTarotCard>.ValueCount, ~soulMaskDbl
            );

            tarots = Vector256.Create((int)MotelyItemTypeCategory.TarotCard) | tarots;

            int resampleCount = 0;

            while (true)
            {
                Vector256<int> resampleMaskInt = itemSet.Contains(new MotelyItemVector(tarots));

                // Don't resmaple lanes which have the soul
                resampleMaskInt &= ~soulMaskInt;

                if (Vector256.EqualsAll(resampleMaskInt, Vector256<int>.Zero))
                    break;

                Vector256<int> nextTarots = GetNextRandomInt(
                    ref GetResamplePrngStream(ref tarotStream.ResampleStream, tarotStream.ResampleKey, resampleCount),
                    0, MotelyEnum<MotelyTarotCard>.ValueCount, MotelyVectorUtils.ExtendIntMaskToDouble(resampleMaskInt)
                );

                nextTarots = Vector256.Create((int)MotelyItemTypeCategory.TarotCard) | nextTarots;

                tarots = Vector256.ConditionalSelect(
                    resampleMaskInt,
                    nextTarots, tarots
                );

                ++resampleCount;
            }
        }

        return new(Vector256.ConditionalSelect(
            soulMaskInt,
            Vector256.Create(new MotelyItem(MotelyItemType.Soul).Value),
            tarots
        ));
    }
}