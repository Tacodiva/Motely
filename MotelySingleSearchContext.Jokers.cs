
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Motely;

public ref struct MotelySingleJokerStream
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

public ref struct MotelySingleJokerFixedRarityStream
{
    public MotelyJokerRarity Rarity;
    public MotelySinglePrngStream EditionPrngStream;
    public MotelySinglePrngStream EternalPerishablePrngStream;
    public MotelySinglePrngStream RentalPrngStream;
    public MotelySinglePrngStream JokerPrngStream;
}

[Flags]
public enum MotelyJokerStreamFlags {
    ExcludeStickers = 1 << 1,
    ExcludeEdition = 1 << 2,

    ExcludeCommonJokers = 1 << 3,
    ExcludeUncommonJokers = 1 << 4,
    ExcludeRareJokers = 1 << 5
}

unsafe ref partial struct MotelySingleSearchContext
{

    public MotelySingleJokerStream CreateShopJokerStream(int ante, MotelyJokerStreamFlags flags = 0)
    {
        return CreateJokerStream(
            MotelyPrngKeys.ShopItemSource,
            MotelyPrngKeys.ShopJokerEternalPerishableSource,
            MotelyPrngKeys.ShopJokerRentalSource,
            ante, flags
        );
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySingleJokerStream CreateJokerStream(string source, string eternalPerishableSource, string rentalSource, int ante, MotelyJokerStreamFlags flags)
    {
        return new()
        {
            StreamSuffix = source + ante,
            RarityPrngStream = CreatePrngStream(MotelyPrngKeys.JokerRarity + ante + source),
            EditionPrngStream = !flags.HasFlag(MotelyJokerStreamFlags.ExcludeEdition) ? CreatePrngStream(MotelyPrngKeys.JokerEdition + source + ante) : MotelySinglePrngStream.Invalid,
            EternalPerishablePrngStream = (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Black) ? CreatePrngStream(eternalPerishableSource + ante) : MotelySinglePrngStream.Invalid,
            RentalPrngStream = (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeStickers) && Stake >= MotelyStake.Gold) ? CreatePrngStream(rentalSource + ante) : MotelySinglePrngStream.Invalid,
            CommonJokerPrngStream = new(flags.HasFlag(MotelyJokerStreamFlags.ExcludeCommonJokers) ? -2 : -1),
            UncommonJokerPrngStream = new(flags.HasFlag(MotelyJokerStreamFlags.ExcludeUncommonJokers) ? -2 : -1),
            RareJokerPrngStream = new(flags.HasFlag(MotelyJokerStreamFlags.ExcludeRareJokers) ? -2 : -1),
        };
    }

    public MotelySingleJokerFixedRarityStream CreateSoulJokerStream(int ante)
    {
        return CreateJokerFixedRarityStream(
            MotelyPrngKeys.JokerSoul,
            MotelyPrngKeys.ShopJokerEternalPerishableSource,
            MotelyPrngKeys.ShopJokerRentalSource,
            ante, MotelyJokerRarity.Legendary
        );
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySingleJokerFixedRarityStream CreateJokerFixedRarityStream(string source, string eternalPerishableSource, string rentalSource, int ante, MotelyJokerRarity rarity)
    {
        string jokerStreamKey = rarity switch
        {
            MotelyJokerRarity.Common => MotelyPrngKeys.JokerCommon + source + ante,
            MotelyJokerRarity.Uncommon => MotelyPrngKeys.JokerUncommon + source + ante,
            MotelyJokerRarity.Rare => MotelyPrngKeys.JokerRare + source + ante,
            MotelyJokerRarity.Legendary => MotelyPrngKeys.JokerLegendary,
            _ => throw new InvalidEnumArgumentException()
        };

        return new()
        {
            Rarity = rarity,
            EditionPrngStream = CreatePrngStream(MotelyPrngKeys.JokerEdition + source + ante),
            EternalPerishablePrngStream = Stake >= MotelyStake.Black ? CreatePrngStream(eternalPerishableSource + ante) : MotelySinglePrngStream.Invalid,
            RentalPrngStream = Stake >= MotelyStake.Gold ? CreatePrngStream(rentalSource + ante) : MotelySinglePrngStream.Invalid,
            JokerPrngStream = CreatePrngStream(jokerStreamKey)
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

        if (stickerPoll > 0.7)
        {
            item = item.WithEternal(true);
        }

        if (Stake < MotelyStake.Orange) return item;

        if (stickerPoll > 0.4 && stickerPoll <= 0.7)
        {
            item = item.WithPerishable(true);
        }

        if (Stake < MotelyStake.Gold) return item;

        Debug.Assert(!rentalStream.IsInvalid);

        stickerPoll = GetNextRandom(ref rentalStream);

        if (stickerPoll > 0.7)
        {
            // Rental Sticker
            item = item.WithRental(true);
        }

        return item;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyItem GetNextJoker(ref MotelySingleJokerFixedRarityStream stream)
    {
        MotelyItem joker = GetNextJoker(stream.Rarity, GetNextEdition(ref stream.EditionPrngStream, 1), ref stream.JokerPrngStream);

        return ApplyNextStickers(joker, ref stream.EternalPerishablePrngStream, ref stream.RentalPrngStream);
    }

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyItem GetNextJoker(ref MotelySingleJokerStream stream)
    {
        MotelyJokerRarity rarity;

        double rarityPoll = GetNextRandom(ref stream.RarityPrngStream);

        if (rarityPoll > 0.95)
        {
            if (!stream.DoesProvideRareJokers)
                return new(MotelyItemType.JokerExcludedByStream);

            rarity = MotelyJokerRarity.Rare;
        }
        else if (rarityPoll > 0.7)
        {
            if (!stream.DoesProvideUncommonJokers)
                return new(MotelyItemType.JokerExcludedByStream);

            rarity = MotelyJokerRarity.Uncommon;
        }
        else
        {
            if (!stream.DoesProvideCommonJokers)
                return new(MotelyItemType.JokerExcludedByStream);

            rarity = MotelyJokerRarity.Common;
        }

        MotelyItemEdition edition;

        if (stream.DoesProvideEdition)
        {
            edition = GetNextEdition(ref stream.EditionPrngStream, 1);
        }
        else
        {
            edition = MotelyItemEdition.None;
        }

        // Get next joker
        MotelyJoker joker;

        if (rarity == MotelyJokerRarity.Rare)
        {
            if (stream.RareJokerPrngStream.IsInvalid)
                stream.RareJokerPrngStream = CreatePrngStream(MotelyPrngKeys.JokerRare + stream.StreamSuffix);

            joker = (MotelyJoker)((int)MotelyJokerRarity.Rare | (int)GetNextJoker<MotelyJokerRare>(ref stream.RareJokerPrngStream));
        }
        else if (rarity == MotelyJokerRarity.Uncommon)
        {
            if (stream.UncommonJokerPrngStream.IsInvalid)
                stream.UncommonJokerPrngStream = CreatePrngStream(MotelyPrngKeys.JokerUncommon + stream.StreamSuffix);

            joker = (MotelyJoker)((int)MotelyJokerRarity.Uncommon | (int)GetNextJoker<MotelyJokerUncommon>(ref stream.UncommonJokerPrngStream));
        }
        else
        {
            Debug.Assert(rarity == MotelyJokerRarity.Common);

            if (stream.CommonJokerPrngStream.IsInvalid)
                stream.CommonJokerPrngStream = CreatePrngStream(MotelyPrngKeys.JokerCommon + stream.StreamSuffix);

            joker = (MotelyJoker)((int)MotelyJokerRarity.Common | (int)GetNextJoker<MotelyJokerCommon>(ref stream.CommonJokerPrngStream));
        }

        MotelyItem jokerItem = new(joker, edition);

        if (stream.DoesProvideStickers)
        {
            jokerItem = ApplyNextStickers(new(joker, edition), ref stream.EternalPerishablePrngStream, ref stream.RentalPrngStream);
        }

        return jokerItem;
    }


#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyItem GetNextJoker(MotelyJokerRarity rarity, MotelyItemEdition edition, ref MotelySinglePrngStream stream)
    {
        MotelyJoker joker;

        switch (rarity)
        {
            case MotelyJokerRarity.Legendary:
                joker = (MotelyJoker)((int)MotelyJokerRarity.Legendary | (int)GetNextJoker<MotelyJokerLegendary>(ref stream));
                break;
            case MotelyJokerRarity.Rare:
                joker = (MotelyJoker)((int)MotelyJokerRarity.Rare | (int)GetNextJoker<MotelyJokerRare>(ref stream));
                break;
            case MotelyJokerRarity.Uncommon:
                joker = (MotelyJoker)((int)MotelyJokerRarity.Uncommon | (int)GetNextJoker<MotelyJokerUncommon>(ref stream));
                break;
            default:
                Debug.Assert(rarity == MotelyJokerRarity.Common);
                joker = (MotelyJoker)((int)MotelyJokerRarity.Common | (int)GetNextJoker<MotelyJokerCommon>(ref stream));
                break;
        }

        return new(joker, edition);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private T GetNextJoker<T>(ref MotelySinglePrngStream stream) where T : unmanaged, Enum
    {
        Debug.Assert(sizeof(T) == 4);
        int value = GetNextRandomInt(ref stream, 0, MotelyEnum<T>.ValueCount);
        return Unsafe.As<int, T>(ref value);
    }
}