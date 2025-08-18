
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

    public readonly bool DoesProvideCommonJokers => CommonJokerPrngStream.State != -2;
    public readonly bool DoesProvideUncommonJokers => UncommonJokerPrngStream.State != -2;
    public readonly bool DoesProvideRareJokers => RareJokerPrngStream.State != -2;
    public readonly bool DoesProvideEdition => !EditionPrngStream.IsInvalid;
    public readonly bool DoesProvideStickers => !EternalPerishablePrngStream.IsInvalid;
}

public struct MotelySingleJokerFixedRarityStream
{
    public MotelyJokerRarity Rarity;
    public MotelySinglePrngStream EditionPrngStream;
    public MotelySinglePrngStream EternalPerishablePrngStream;
    public MotelySinglePrngStream RentalPrngStream;
    public MotelySinglePrngStream JokerPrngStream;

    public readonly bool DoesProvideEdition => !EditionPrngStream.IsInvalid;
    public readonly bool DoesProvideStickers => !EternalPerishablePrngStream.IsInvalid;
}

[Flags]
public enum MotelyJokerStreamFlags
{
    ExcludeStickers = 1 << 1,
    ExcludeEdition = 1 << 2,

    ExcludeCommonJokers = 1 << 3,
    ExcludeUncommonJokers = 1 << 4,
    ExcludeRareJokers = 1 << 5,

    Default = 0
}

unsafe ref partial struct MotelySingleSearchContext
{

    public MotelySingleJokerStream CreateShopJokerStream(int ante, MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default, bool isCached = false)
    {
        return CreateJokerStream(
            MotelyPrngKeys.ShopItemSource,
            MotelyPrngKeys.ShopJokerEternalPerishableSource,
            MotelyPrngKeys.ShopJokerRentalSource,
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

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySingleJokerStream CreateJokerStream(string source, string eternalPerishableSource, string rentalSource, int ante, MotelyJokerStreamFlags flags, bool isCached)
    {
        return new()
        {
            StreamSuffix = source + ante,
            RarityPrngStream = CreatePrngStream(MotelyPrngKeys.JokerRarity + ante + source, isCached),
            EditionPrngStream = !flags.HasFlag(MotelyJokerStreamFlags.ExcludeEdition) ?
                CreatePrngStream(MotelyPrngKeys.JokerEdition + source + ante, isCached) : MotelySinglePrngStream.Invalid,
            EternalPerishablePrngStream = (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Black) ?
                CreatePrngStream(eternalPerishableSource + ante, isCached) : MotelySinglePrngStream.Invalid,
            RentalPrngStream = (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Gold) ?
                CreatePrngStream(rentalSource + ante, isCached) : MotelySinglePrngStream.Invalid,
            CommonJokerPrngStream = new(flags.HasFlag(MotelyJokerStreamFlags.ExcludeCommonJokers) ? -2 : -1),
            UncommonJokerPrngStream = new(flags.HasFlag(MotelyJokerStreamFlags.ExcludeUncommonJokers) ? -2 : -1),
            RareJokerPrngStream = new(flags.HasFlag(MotelyJokerStreamFlags.ExcludeRareJokers) ? -2 : -1),
        };
    }

    public MotelySingleJokerFixedRarityStream CreateSoulJokerStream(int ante, MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default, bool isCached = false)
    {
        return CreateJokerFixedRarityStream(
            MotelyPrngKeys.JokerSoulSource,
            MotelyPrngKeys.ShopJokerEternalPerishableSource,
            MotelyPrngKeys.ShopJokerRentalSource,
            ante, flags, MotelyJokerRarity.Legendary, isCached
        );
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySingleJokerFixedRarityStream CreateJokerFixedRarityStream(string source, string eternalPerishableSource, string rentalSource, int ante, MotelyJokerStreamFlags flags, MotelyJokerRarity rarity, bool isCached)
    {
        return new()
        {
            Rarity = rarity,
            JokerPrngStream = CreatePrngStream(MotelyPrngKeys.FixedRarityJoker(rarity, source, ante), isCached),
            EditionPrngStream = flags.HasFlag(MotelyJokerStreamFlags.ExcludeEdition) ?
                CreatePrngStream(MotelyPrngKeys.JokerEdition + source + ante, isCached) : MotelySinglePrngStream.Invalid,
            EternalPerishablePrngStream = (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Black) ?
                CreatePrngStream(eternalPerishableSource + ante, isCached) : MotelySinglePrngStream.Invalid,
            RentalPrngStream = (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Gold) ?
                CreatePrngStream(rentalSource + ante, isCached) : MotelySinglePrngStream.Invalid,
        };
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
    private MotelyItem ApplyNextStickers(MotelyItem item, ref MotelySinglePrngStream eternalPerishableStream, ref MotelySinglePrngStream rentalStream)
    {
        if (Stake < MotelyStake.Black) return item;

        Debug.Assert(!eternalPerishableStream.IsInvalid);

        double stickerPoll = GetNextRandom(ref eternalPerishableStream);

        item = item.WithEternal(stickerPoll > 0.7);

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
    {
        MotelyJoker joker;

        double rarityPoll = GetNextRandom(ref stream.RarityPrngStream);

        if (rarityPoll > 0.95)
        {
            if (!stream.DoesProvideRareJokers)
                return new(MotelyItemType.JokerExcludedByStream);

            if (stream.RareJokerPrngStream.IsInvalid)
                stream.RareJokerPrngStream = CreatePrngStream(MotelyPrngKeys.JokerRare + stream.StreamSuffix);

            joker = GetNextJoker<MotelyJokerRare>(ref stream.RareJokerPrngStream, MotelyJokerRarity.Rare);
        }
        else if (rarityPoll > 0.7)
        {
            if (!stream.DoesProvideUncommonJokers)
                return new(MotelyItemType.JokerExcludedByStream);

            if (stream.UncommonJokerPrngStream.IsInvalid)
                stream.UncommonJokerPrngStream = CreatePrngStream(MotelyPrngKeys.JokerUncommon + stream.StreamSuffix);

            joker = GetNextJoker<MotelyJokerUncommon>(ref stream.UncommonJokerPrngStream, MotelyJokerRarity.Uncommon);
        }
        else
        {
            if (!stream.DoesProvideCommonJokers)
                return new(MotelyItemType.JokerExcludedByStream);

            if (stream.CommonJokerPrngStream.IsInvalid)
                stream.CommonJokerPrngStream = CreatePrngStream(MotelyPrngKeys.JokerCommon + stream.StreamSuffix);

            joker = GetNextJoker<MotelyJokerCommon>(ref stream.CommonJokerPrngStream, MotelyJokerRarity.Common);
        }

        MotelyItem jokerItem = new(joker);

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