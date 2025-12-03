
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;


public ref struct MotelyVectorJokerStream
{
    public readonly bool IsNull => StreamSuffix == null;

    public string StreamSuffix;
    public MotelyVectorPrngStream EditionPrngStream;
    public MotelyVectorPrngStream RarityPrngStream;
    public MotelyVectorPrngStream EternalPerishablePrngStream;
    public MotelyVectorPrngStream RentalPrngStream;

    // For these, a state set to -1 means they are not yet initialized.
    //  A state of -2 means the stream does not provide that joker
    public MotelyVectorPrngStream CommonJokerPrngStream;
    public MotelyVectorPrngStream UncommonJokerPrngStream;
    public MotelyVectorPrngStream RareJokerPrngStream;

    public readonly bool DoesProvideCommonJokers => !CommonJokerPrngStream.IsInvalid;
    public readonly bool DoesProvideUncommonJokers => !UncommonJokerPrngStream.IsInvalid;
    public readonly bool DoesProvideRareJokers => !RareJokerPrngStream.IsInvalid;
    public readonly bool DoesProvideEdition => !EditionPrngStream.IsInvalid;
    public readonly bool DoesProvideStickers => !EternalPerishablePrngStream.IsInvalid;
}

public struct MotelyVectorJokerFixedRarityStream
{
    public MotelyJokerRarity Rarity;
    public MotelyVectorPrngStream EditionPrngStream;
    public MotelyVectorPrngStream EternalPerishablePrngStream;
    public MotelyVectorPrngStream RentalPrngStream;
    public MotelyVectorPrngStream JokerPrngStream;

    public readonly bool DoesProvideEdition => !EditionPrngStream.IsInvalid;
    public readonly bool DoesProvideStickers => !EternalPerishablePrngStream.IsInvalid;
}

unsafe partial struct MotelyVectorSearchContext
{

    public MotelyVectorJokerStream CreateShopJokerStream(int ante, MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default, bool isCached = false)
    {
        return CreateJokerStream(
            MotelyPrngKeys.ShopItemSource,
            MotelyPrngKeys.DefaultJokerEternalPerishableSource,
            MotelyPrngKeys.DefaultJokerRentalSource,
            ante, flags, isCached
        );
    }

    public MotelyVectorJokerStream CreateJudgementJokerStream(int ante, MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default, bool isCached = false)
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
    private MotelyVectorJokerStream CreateJokerStream(string source, string eternalPerishableSource, string rentalSource, int ante, MotelyJokerStreamFlags flags, bool isCached)
    {
        string streamSuffix = source + ante;

        return new()
        {
            StreamSuffix = streamSuffix,
            RarityPrngStream = CreatePrngStream(MotelyPrngKeys.JokerRarity + ante + source, isCached),
            EditionPrngStream = !flags.HasFlag(MotelyJokerStreamFlags.ExcludeEdition) ?
                CreatePrngStream(MotelyPrngKeys.JokerEdition + streamSuffix, isCached) : MotelyVectorPrngStream.Invalid,
            EternalPerishablePrngStream = (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Black) ?
                CreatePrngStream(eternalPerishableSource + ante, isCached) : MotelyVectorPrngStream.Invalid,
            RentalPrngStream = (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Gold) ?
                CreatePrngStream(rentalSource + ante, isCached) : MotelyVectorPrngStream.Invalid,
            CommonJokerPrngStream = !flags.HasFlag(MotelyJokerStreamFlags.ExcludeCommonJokers) ?
                CreatePrngStream(MotelyPrngKeys.JokerCommon + streamSuffix) : MotelyVectorPrngStream.Invalid,
            UncommonJokerPrngStream = !flags.HasFlag(MotelyJokerStreamFlags.ExcludeUncommonJokers) ?
                CreatePrngStream(MotelyPrngKeys.JokerUncommon + streamSuffix) : MotelyVectorPrngStream.Invalid,
            RareJokerPrngStream = !flags.HasFlag(MotelyJokerStreamFlags.ExcludeRareJokers) ?
                CreatePrngStream(MotelyPrngKeys.JokerRare + streamSuffix) : MotelyVectorPrngStream.Invalid,
        };
    }

    public MotelyVectorJokerFixedRarityStream CreateSoulJokerStream(int ante, MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default, bool isCached = false)
    {
        return CreateJokerFixedRarityStream(
            MotelyPrngKeys.JokerSoulSource,
            MotelyPrngKeys.DefaultJokerEternalPerishableSource,
            MotelyPrngKeys.DefaultJokerRentalSource,
            ante, flags, MotelyJokerRarity.Legendary, isCached
        );
    }

    public MotelyVectorJokerFixedRarityStream CreateRareTagJokerStream(int ante, MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default, bool isCached = false)
    {
        return CreateJokerFixedRarityStream(
            MotelyPrngKeys.TagRare,
            MotelyPrngKeys.DefaultJokerEternalPerishableSource,
            MotelyPrngKeys.DefaultJokerRentalSource,
            ante, flags, MotelyJokerRarity.Rare, isCached
        );
    }

    public MotelyVectorJokerFixedRarityStream CreateUncommonTagJokerStream(int ante, MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default, bool isCached = false)
    {
        return CreateJokerFixedRarityStream(
            MotelyPrngKeys.TagUncommon,
            MotelyPrngKeys.DefaultJokerEternalPerishableSource,
            MotelyPrngKeys.DefaultJokerRentalSource,
            ante, flags, MotelyJokerRarity.Uncommon, isCached
        );
    }

    public MotelyVectorJokerFixedRarityStream CreateRiffRaffJokerStream(int ante, MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default, bool isCached = false)
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
    private MotelyVectorJokerFixedRarityStream CreateJokerFixedRarityStream(string source, string eternalPerishableSource, string rentalSource, int ante, MotelyJokerStreamFlags flags, MotelyJokerRarity rarity, bool isCached)
    {
        return new()
        {
            Rarity = rarity,
            JokerPrngStream = CreatePrngStream(MotelyPrngKeys.FixedRarityJoker(rarity, source, ante), isCached),
            EditionPrngStream = !flags.HasFlag(MotelyJokerStreamFlags.ExcludeEdition) ?
                CreatePrngStream(MotelyPrngKeys.JokerEdition + source + ante, isCached) : MotelyVectorPrngStream.Invalid,
            EternalPerishablePrngStream = (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Black) ?
                CreatePrngStream(eternalPerishableSource + ante, isCached) : MotelyVectorPrngStream.Invalid,
            RentalPrngStream = (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Gold) ?
                CreatePrngStream(rentalSource + ante, isCached) : MotelyVectorPrngStream.Invalid,
        };
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private VectorEnum256<MotelyItemEdition> GetNextEdition(ref MotelyVectorPrngStream stream, int editionRate)
    {
        Vector512<double> editionPoll = GetNextRandom(ref stream);

        // O_O
        return new(
            Vector256.ConditionalSelect(
                MotelyVectorUtils.ShrinkDoubleMaskToInt(Vector512.GreaterThan(editionPoll, Vector512.Create(0.997))),
                Vector256.Create((int)MotelyItemEdition.Negative),
            Vector256.ConditionalSelect(
                MotelyVectorUtils.ShrinkDoubleMaskToInt(Vector512.GreaterThan(editionPoll, Vector512.Create(1 - 0.006 * editionRate))),
                Vector256.Create((int)MotelyItemEdition.Polychrome),
            Vector256.ConditionalSelect(
                MotelyVectorUtils.ShrinkDoubleMaskToInt(Vector512.GreaterThan(editionPoll, Vector512.Create(1 - 0.02 * editionRate))),
                Vector256.Create((int)MotelyItemEdition.Holographic),
            Vector256.ConditionalSelect(
                MotelyVectorUtils.ShrinkDoubleMaskToInt(Vector512.GreaterThan(editionPoll, Vector512.Create(1 - 0.04 * editionRate))),
                Vector256.Create((int)MotelyItemEdition.Foil),
                Vector256.Create((int)MotelyItemEdition.None)
        )))));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyItemVector ApplyNextStickers(MotelyItemVector item, ref MotelyVectorPrngStream eternalPerishableStream, ref MotelyVectorPrngStream rentalStream)
    {
        if (Stake < MotelyStake.Black) return item;

        Debug.Assert(!eternalPerishableStream.IsInvalid);

        Vector512<double> stickerPoll = GetNextRandom(ref eternalPerishableStream);

        Vector256<int> eternalMask = MotelyVectorUtils.ShrinkDoubleMaskToInt(Vector512.GreaterThan(stickerPoll, Vector512.Create(0.7)));
        item = item.WithEternal(eternalMask);

        if (Stake < MotelyStake.Orange) return item;

        Vector256<int> perishableMask = ~eternalMask & MotelyVectorUtils.ShrinkDoubleMaskToInt(Vector512.GreaterThan(stickerPoll, Vector512.Create(0.4)));
        item = item.WithPerishable(perishableMask);

        if (Stake < MotelyStake.Gold) return item;

        Debug.Assert(!rentalStream.IsInvalid);

        stickerPoll = GetNextRandom(ref rentalStream);

        Vector256<int> rentallMask = MotelyVectorUtils.ShrinkDoubleMaskToInt(Vector512.GreaterThan(stickerPoll, Vector512.Create(0.7)));
        item = item.WithRental(rentallMask);

        return item;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyItemVector GetNextJoker(ref MotelyVectorJokerFixedRarityStream stream)
    {

        MotelyItemVector item;

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
    public MotelyItemVector GetNextJoker(ref MotelyVectorJokerStream stream)
    {

        MotelyItemVector jokers;

        // Pick the joker
        {
            Vector512<double> rarityPoll = GetNextRandom(ref stream.RarityPrngStream);

            Vector512<double> rareMask = Vector512.GreaterThan(rarityPoll, Vector512.Create(0.95));
            Vector512<double> uncommonMask = ~rareMask & Vector512.GreaterThan(rarityPoll, Vector512.Create(0.7));
            Vector512<double> commonMask = ~rareMask & ~uncommonMask;

            Vector256<int> rareJokers = stream.DoesProvideRareJokers ?
                GetNextJoker<MotelyJokerRare>(ref stream.RareJokerPrngStream, MotelyJokerRarity.Rare, rareMask) :
                Vector256.Create(new MotelyItem(MotelyItemType.JokerExcludedByStream).Value);

            Vector256<int> uncommonJokers = stream.DoesProvideUncommonJokers ?
                GetNextJoker<MotelyJokerUncommon>(ref stream.UncommonJokerPrngStream, MotelyJokerRarity.Uncommon, uncommonMask) :
                Vector256.Create(new MotelyItem(MotelyItemType.JokerExcludedByStream).Value);

            Vector256<int> commonJokers = stream.DoesProvideCommonJokers ?
                GetNextJoker<MotelyJokerCommon>(ref stream.CommonJokerPrngStream, MotelyJokerRarity.Common, commonMask) :
                Vector256.Create(new MotelyItem(MotelyItemType.JokerExcludedByStream).Value);

            jokers = new(Vector256.Create((int)MotelyItemTypeCategory.Joker) | Vector256.ConditionalSelect(
                MotelyVectorUtils.ShrinkDoubleMaskToInt(rareMask),
                rareJokers,
                Vector256.ConditionalSelect(
                    MotelyVectorUtils.ShrinkDoubleMaskToInt(uncommonMask),
                    uncommonJokers, commonJokers
                )
            ));
        }

        if (stream.DoesProvideEdition)
        {
            jokers = new(jokers.Value | GetNextEdition(ref stream.EditionPrngStream, 1).HardwareVector);
        }

        if (stream.DoesProvideStickers)
        {
            jokers = ApplyNextStickers(jokers, ref stream.EternalPerishablePrngStream, ref stream.RentalPrngStream);
        }

        return jokers;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private Vector256<int> GetNextJoker<T>(ref MotelyVectorPrngStream stream, MotelyJokerRarity rarity, Vector512<double> mask) where T : unmanaged, Enum
    {
        Debug.Assert(sizeof(T) == 4);
        return Vector256.BitwiseOr(Vector256.Create((int)rarity), GetNextRandomInt(ref stream, 0, MotelyEnum<T>.ValueCount, mask));
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private Vector256<int> GetNextJoker<T>(ref MotelyVectorPrngStream stream, MotelyJokerRarity rarity) where T : unmanaged, Enum
    {
        Debug.Assert(sizeof(T) == 4);
        return Vector256.BitwiseOr(Vector256.Create((int)rarity), GetNextRandomInt(ref stream, 0, MotelyEnum<T>.ValueCount));
    }
}