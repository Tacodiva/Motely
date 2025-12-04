
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Motely;

public struct MotelySingleJokerStream
{
    public readonly bool IsNull => StreamSuffix == null;

    public string StreamSuffix;
    public MotelySinglePrngStream EditionPrngStream;
    public MotelySinglePrngStream RarityPrngStream;
    public MotelySinglePrngStream EternalPerishablePrngStream;
    public MotelySinglePrngStream RentalPrngStream;

    // For these, a state set to -1 means they are not yet initialized.
    //  A state of -2 means the stream does not provide that joker
    public MotelySinglePrngStream CommonJokerPrngStream;
    public MotelySinglePrngStream UncommonJokerPrngStream;
    public MotelySinglePrngStream RareJokerPrngStream;

    public readonly bool DoesProvideJokerType => !RarityPrngStream.IsInvalid;
    public readonly bool DoesProvideCommonJokers => CommonJokerPrngStream.State != -2;
    public readonly bool DoesProvideUncommonJokers => UncommonJokerPrngStream.State != -2;
    public readonly bool DoesProvideRareJokers => RareJokerPrngStream.State != -2;
    public readonly bool DoesProvideEdition => !EditionPrngStream.IsInvalid;
    public readonly bool DoesProvideStickers => !EternalPerishablePrngStream.IsInvalid;

    public MotelySingleJokerResampleStreams? ResampleStreams;

}

public class MotelySingleJokerResampleStreams
{
    public MotelySingleResampleStream CommonJokerResampleStream = MotelySingleResampleStream.Invalid;
    public MotelySingleResampleStream UncommonJokerResampleStream = MotelySingleResampleStream.Invalid;
    public MotelySingleResampleStream RareJokerResampleStream = MotelySingleResampleStream.Invalid;
}

public struct MotelySingleJokerFixedRarityStream
{
    public MotelyJokerRarity Rarity;
    public MotelySinglePrngStream EditionPrngStream;
    public MotelySinglePrngStream EternalPerishablePrngStream;
    public MotelySinglePrngStream RentalPrngStream;
    public MotelySinglePrngStream JokerPrngStream;

    public readonly bool DoesProvideJokerType => !JokerPrngStream.IsInvalid;
    public readonly bool DoesProvideEdition => !EditionPrngStream.IsInvalid;
    public readonly bool DoesProvideStickers => !EternalPerishablePrngStream.IsInvalid;
}

[Flags]
public enum MotelyJokerStreamFlags
{
    ExcludeStickers = 1 << 1,
    ExcludeEdition = 1 << 2,

    ExcludeJokerType = 1 << 3,
    ExcludeCommonJokers = 1 << 4,
    ExcludeUncommonJokers = 1 << 5,
    ExcludeRareJokers = 1 << 6,

    Default = 0
}


[Flags]
public enum MotelyJokerFixedRarityStreamFlags
{
    ExcludeStickers = 1 << 1,
    ExcludeEdition = 1 << 2,

    ExcludeJokerType = 1 << 3,

    Default = 0
}

unsafe ref partial struct MotelySingleSearchContext
{

    public MotelySingleJokerStream CreateShopJokerStream(int ante, MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default, bool isCached = false)
    {
        return CreateJokerStream(
            MotelyPrngKeys.ShopItemSource,
            MotelyPrngKeys.DefaultJokerEternalPerishableSource,
            MotelyPrngKeys.DefaultJokerRentalSource,
            ante, flags, isCached
        );
    }

    public MotelySingleJokerStream CreateBuffoonPackJokerStream(int ante, MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default, bool isCached = false)
    {
        // Each pack gets its own stream based on pack index
        return CreateJokerStream(
            MotelyPrngKeys.BuffoonPackItemSource,
            MotelyPrngKeys.BuffoonJokerEternalPerishableSource,
            MotelyPrngKeys.BuffoonJokerRentalSource,
            ante, flags, isCached
        );
    }

    public MotelySingleJokerStream CreateJudgementJokerStream(int ante, MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default, bool isCached = false)
    {
        return CreateJokerStream(
            MotelyPrngKeys.TarotJudgement,
            MotelyPrngKeys.DefaultJokerEternalPerishableSource,
            MotelyPrngKeys.DefaultJokerRentalSource,
            ante, flags, isCached
        );
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySingleJokerStream CreateJokerStream(string source, string eternalPerishableSource, string rentalSource, int ante, MotelyJokerStreamFlags flags, bool isCached)
    {
        return new()
        {
            StreamSuffix = source + ante,
            RarityPrngStream = !flags.HasFlag(MotelyJokerStreamFlags.ExcludeJokerType) ?
                CreatePrngStream(MotelyPrngKeys.JokerRarity + ante + source, isCached) : MotelySinglePrngStream.Invalid,
            EditionPrngStream = !flags.HasFlag(MotelyJokerStreamFlags.ExcludeEdition) ?
                CreatePrngStream(MotelyPrngKeys.JokerEdition + source + ante, isCached) : MotelySinglePrngStream.Invalid,
            EternalPerishablePrngStream = (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Black) ?
                CreatePrngStream(eternalPerishableSource + ante, isCached) : MotelySinglePrngStream.Invalid,
            RentalPrngStream = (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Gold) ?
                CreatePrngStream(rentalSource + ante, isCached) : MotelySinglePrngStream.Invalid,
            CommonJokerPrngStream = new(flags.HasFlag(MotelyJokerStreamFlags.ExcludeCommonJokers) || flags.HasFlag(MotelyJokerStreamFlags.ExcludeJokerType) ? -2 : -1),
            UncommonJokerPrngStream = new(flags.HasFlag(MotelyJokerStreamFlags.ExcludeUncommonJokers) || flags.HasFlag(MotelyJokerStreamFlags.ExcludeJokerType) ? -2 : -1),
            RareJokerPrngStream = new(flags.HasFlag(MotelyJokerStreamFlags.ExcludeRareJokers) || flags.HasFlag(MotelyJokerStreamFlags.ExcludeJokerType) ? -2 : -1),
            ResampleStreams = null,
        };
    }

    public MotelySingleJokerFixedRarityStream CreateSoulJokerStream(int ante, MotelyJokerFixedRarityStreamFlags flags = MotelyJokerFixedRarityStreamFlags.Default, bool isCached = false)
    {
        return CreateJokerFixedRarityStream(
            MotelyPrngKeys.JokerSoulSource,
            MotelyPrngKeys.DefaultJokerEternalPerishableSource,
            MotelyPrngKeys.DefaultJokerRentalSource,
            ante, flags, MotelyJokerRarity.Legendary, isCached
        );
    }

    public MotelySingleJokerFixedRarityStream CreateRareTagJokerStream(int ante, MotelyJokerFixedRarityStreamFlags flags = MotelyJokerFixedRarityStreamFlags.Default, bool isCached = false)
    {
        return CreateJokerFixedRarityStream(
            MotelyPrngKeys.TagRare,
            MotelyPrngKeys.DefaultJokerEternalPerishableSource,
            MotelyPrngKeys.DefaultJokerRentalSource,
            ante, flags, MotelyJokerRarity.Rare, isCached
        );
    }

    public MotelySingleJokerFixedRarityStream CreateUncommonTagJokerStream(int ante, MotelyJokerFixedRarityStreamFlags flags = MotelyJokerFixedRarityStreamFlags.Default, bool isCached = false)
    {
        return CreateJokerFixedRarityStream(
            MotelyPrngKeys.TagUncommon,
            MotelyPrngKeys.DefaultJokerEternalPerishableSource,
            MotelyPrngKeys.DefaultJokerRentalSource,
            ante, flags, MotelyJokerRarity.Uncommon, isCached
        );
    }

    public MotelySingleJokerFixedRarityStream CreateRiffRaffJokerStream(int ante, MotelyJokerFixedRarityStreamFlags flags = MotelyJokerFixedRarityStreamFlags.Default, bool isCached = false)
    {
        return CreateJokerFixedRarityStream(
            MotelyPrngKeys.JokerRiffRaff,
            MotelyPrngKeys.DefaultJokerEternalPerishableSource,
            MotelyPrngKeys.DefaultJokerRentalSource,
            ante, flags, MotelyJokerRarity.Common, isCached
        );
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySingleJokerFixedRarityStream CreateJokerFixedRarityStream(string source, string eternalPerishableSource, string rentalSource, int ante, MotelyJokerFixedRarityStreamFlags flags, MotelyJokerRarity rarity, bool isCached)
    {
        return new()
        {
            Rarity = rarity,
            JokerPrngStream = !flags.HasFlag(MotelyJokerFixedRarityStreamFlags.ExcludeJokerType) ?
                CreatePrngStream(MotelyPrngKeys.FixedRarityJoker(rarity, source, ante), isCached) : MotelySinglePrngStream.Invalid,
            EditionPrngStream = !flags.HasFlag(MotelyJokerFixedRarityStreamFlags.ExcludeEdition) ?
                CreatePrngStream(MotelyPrngKeys.JokerEdition + source + ante, isCached) : MotelySinglePrngStream.Invalid,
            EternalPerishablePrngStream = (!flags.HasFlag(MotelyJokerFixedRarityStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Black) ?
                CreatePrngStream(eternalPerishableSource + ante, isCached) : MotelySinglePrngStream.Invalid,
            RentalPrngStream = (!flags.HasFlag(MotelyJokerFixedRarityStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Gold) ?
                CreatePrngStream(rentalSource + ante, isCached) : MotelySinglePrngStream.Invalid,
        };
    }

    public MotelySingleItemSet GetNextBuffoonPackContents(ref MotelySingleJokerStream jokerStream, MotelyBoosterPackSize size)
    => GetNextBuffoonPackContents(ref jokerStream, MotelyBoosterPackType.Buffoon.GetCardCount(size));

    public MotelySingleItemSet GetNextBuffoonPackContents(ref MotelySingleJokerStream jokerStream, int size)
    {
        Debug.Assert(size <= MotelySingleItemSet.MaxLength);

        MotelySingleItemSet pack = new();

        for (int i = 0; i < size; i++)
            pack.Append(GetNextJoker(ref jokerStream, ref pack));

        return pack;
    }


#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyItemEdition GetNextEdition(ref MotelySinglePrngStream stream, int editionRate)
    {
        double editionPoll = GetNextRandom(ref stream);

        if (editionPoll > 0.997)
            return MotelyItemEdition.Negative;
        else if (editionPoll > 1 - 0.006 * editionRate)
            return MotelyItemEdition.Polychrome;
        else if (editionPoll > 1 - 0.02 * editionRate)
            return MotelyItemEdition.Holographic;
        else if (editionPoll > 1 - 0.04 * editionRate)
            return MotelyItemEdition.Foil;
        else
            return MotelyItemEdition.None;
    }


#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private static bool CanBeEternal(MotelyItem item)
    {
        // Jokers that self-destruct or activate on sell cannot receive the Eternal Sticker
        MotelyItemType joker = item.Type;
        return joker != MotelyItemType.Cavendish &&
               joker != MotelyItemType.DietCola &&
               joker != MotelyItemType.GrosMichel &&
               joker != MotelyItemType.IceCream &&
               joker != MotelyItemType.InvisibleJoker &&
               joker != MotelyItemType.Luchador &&
               joker != MotelyItemType.MrBones &&
               joker != MotelyItemType.Popcorn &&
               joker != MotelyItemType.Ramen &&
               joker != MotelyItemType.Seltzer &&
               joker != MotelyItemType.TurtleBean;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyItem ApplyNextStickers(MotelyItem item, ref MotelySinglePrngStream eternalPerishableStream, ref MotelySinglePrngStream rentalStream)
    {
        if (Stake < MotelyStake.Black) return item;

        Debug.Assert(!eternalPerishableStream.IsInvalid);

        double stickerPoll = GetNextRandom(ref eternalPerishableStream);

        item = item.WithEternal(stickerPoll > 0.7 && CanBeEternal(item));

        if (Stake < MotelyStake.Orange) return item;

        item = item.WithPerishable(stickerPoll > 0.4 && stickerPoll <= 0.7);

        if (Stake < MotelyStake.Gold) return item;

        Debug.Assert(!rentalStream.IsInvalid);

        stickerPoll = GetNextRandom(ref rentalStream);

        item = item.WithRental(stickerPoll > 0.7);

        return item;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyItem GetNextJoker(ref MotelySingleJokerFixedRarityStream stream)
    {

        MotelyItem item;

        if (stream.DoesProvideJokerType)
        {
            switch (stream.Rarity)
            {
                case MotelyJokerRarity.Legendary:
                    item = new(GetNextJoker<MotelyJokerLegendary>(ref stream.JokerPrngStream, MotelyJokerRarity.Legendary));
                    break;
                case MotelyJokerRarity.Rare:
                    item = new(GetNextJoker<MotelyJokerRare>(ref stream.JokerPrngStream, MotelyJokerRarity.Rare));
                    break;
                case MotelyJokerRarity.Uncommon:
                    item = new(GetNextJoker<MotelyJokerUncommon>(ref stream.JokerPrngStream, MotelyJokerRarity.Uncommon));
                    break;
                default:
                    Debug.Assert(stream.Rarity == MotelyJokerRarity.Common);
                    item = new(GetNextJoker<MotelyJokerCommon>(ref stream.JokerPrngStream, MotelyJokerRarity.Common));
                    break;
            }
        }
        else
        {
            item = new(MotelyItemType.JokerExcludedByStream);
        }

        if (stream.DoesProvideEdition)
        {
            item = item.WithEdition(GetNextEdition(ref stream.EditionPrngStream, 1));
        }

        if (stream.DoesProvideStickers)
        {
            item = ApplyNextStickers(item, ref stream.EternalPerishablePrngStream, ref stream.RentalPrngStream);
        }

        return item;
    }

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyItem GetNextJoker(ref MotelySingleJokerStream stream)
        => GetNextJoker(ref stream, ref Unsafe.NullRef<MotelySingleItemSet>());

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyItem GetNextJoker(ref MotelySingleJokerStream stream, ref MotelySingleItemSet items)
    {

        MotelyItem jokerItem;

        if (stream.DoesProvideJokerType)
        {
            MotelyJoker joker;
            MotelyJokerRarity rarity;

            double rarityPoll = GetNextRandom(ref stream.RarityPrngStream);

            if (rarityPoll > 0.95)
            {
                if (!stream.DoesProvideRareJokers)
                    return new(MotelyItemType.JokerExcludedByStream);

                rarity = MotelyJokerRarity.Rare;

                if (stream.RareJokerPrngStream.IsInvalid)
                    stream.RareJokerPrngStream = CreatePrngStream(MotelyPrngKeys.JokerRare + stream.StreamSuffix);

                joker = GetNextJoker<MotelyJokerRare>(ref stream.RareJokerPrngStream, MotelyJokerRarity.Rare);
            }
            else if (rarityPoll > 0.7)
            {
                if (!stream.DoesProvideUncommonJokers)
                    return new(MotelyItemType.JokerExcludedByStream);

                rarity = MotelyJokerRarity.Uncommon;

                if (stream.UncommonJokerPrngStream.IsInvalid)
                    stream.UncommonJokerPrngStream = CreatePrngStream(MotelyPrngKeys.JokerUncommon + stream.StreamSuffix);

                joker = GetNextJoker<MotelyJokerUncommon>(ref stream.UncommonJokerPrngStream, MotelyJokerRarity.Uncommon);
            }
            else
            {
                if (!stream.DoesProvideCommonJokers)
                    return new(MotelyItemType.JokerExcludedByStream);

                rarity = MotelyJokerRarity.Common;

                if (stream.CommonJokerPrngStream.IsInvalid)
                    stream.CommonJokerPrngStream = CreatePrngStream(MotelyPrngKeys.JokerCommon + stream.StreamSuffix);

                joker = GetNextJoker<MotelyJokerCommon>(ref stream.CommonJokerPrngStream, MotelyJokerRarity.Common);
            }

            if (!Unsafe.IsNullRef(ref items))
            {
                if (items.Contains((MotelyItemType)((int)MotelyItemTypeCategory.Joker | (int)joker)))
                {
                    // Resamples!

                    stream.ResampleStreams ??= new();

                    switch (rarity)
                    {
                        case MotelyJokerRarity.Rare:
                            if (stream.ResampleStreams.RareJokerResampleStream.IsInvalid)
                                stream.ResampleStreams.RareJokerResampleStream = CreateResampleStream(MotelyPrngKeys.JokerRare + stream.StreamSuffix, false);
                            break;

                        case MotelyJokerRarity.Uncommon:
                            if (stream.ResampleStreams.UncommonJokerResampleStream.IsInvalid)
                                stream.ResampleStreams.UncommonJokerResampleStream = CreateResampleStream(MotelyPrngKeys.JokerUncommon + stream.StreamSuffix, false);
                            break;

                        default:
                            Debug.Assert(rarity == MotelyJokerRarity.Common);
                            if (stream.ResampleStreams.CommonJokerResampleStream.IsInvalid)
                                stream.ResampleStreams.CommonJokerResampleStream = CreateResampleStream(MotelyPrngKeys.JokerCommon + stream.StreamSuffix, false);
                            break;
                    }

                    int resample = 0;

                    do
                    {
                        switch (rarity)
                        {
                            case MotelyJokerRarity.Rare:
                                joker = GetNextJoker<MotelyJokerRare>(
                                    ref GetResamplePrngStream(
                                        ref stream.ResampleStreams.RareJokerResampleStream,
                                        MotelyPrngKeys.JokerRare + stream.StreamSuffix,
                                        resample),
                                    MotelyJokerRarity.Rare
                                );
                                break;
                            case MotelyJokerRarity.Uncommon:
                                joker = GetNextJoker<MotelyJokerUncommon>(
                                    ref GetResamplePrngStream(
                                        ref stream.ResampleStreams.UncommonJokerResampleStream,
                                        MotelyPrngKeys.JokerUncommon + stream.StreamSuffix,
                                        resample),
                                    MotelyJokerRarity.Uncommon
                                );
                                break;
                            default:
                                Debug.Assert(rarity == MotelyJokerRarity.Common);
                                joker = GetNextJoker<MotelyJokerCommon>(
                                    ref GetResamplePrngStream(
                                        ref stream.ResampleStreams.CommonJokerResampleStream,
                                        MotelyPrngKeys.JokerCommon + stream.StreamSuffix,
                                        resample),
                                    MotelyJokerRarity.Common
                                );
                                break;
                        }
                    } while (items.Contains((MotelyItemType)((int)MotelyItemTypeCategory.Joker | (int)joker)));
                }
            }

            jokerItem = new(joker);
        }
        else
        {
            jokerItem = new(MotelyItemType.JokerExcludedByStream);
        }

        if (stream.DoesProvideEdition)
        {
            jokerItem = jokerItem.WithEdition(GetNextEdition(ref stream.EditionPrngStream, 1));
        }

        if (stream.DoesProvideStickers)
        {
            jokerItem = ApplyNextStickers(jokerItem, ref stream.EternalPerishablePrngStream, ref stream.RentalPrngStream);
        }

        return jokerItem;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyJoker GetNextJoker<T>(ref MotelySinglePrngStream stream, MotelyJokerRarity rarity) where T : unmanaged, Enum
    {
        Debug.Assert(sizeof(T) == 4);
        int value = (int)rarity | GetNextRandomInt(ref stream, 0, MotelyEnum<T>.ValueCount);
        return (MotelyJoker)value;
    }
}