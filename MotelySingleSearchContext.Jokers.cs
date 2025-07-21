
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Motely;

public ref struct MotelySingleJokerStream
{
    public MotelySinglePrngStream EditionPrngStream;
    public MotelySinglePrngStream RarityPrngStream;
    public MotelySinglePrngStream EternalPerishablePrngStream;
    public MotelySinglePrngStream RentalPrngStream;
    public MotelySinglePrngStream CommonJokerPrngStream;
    public MotelySinglePrngStream UncommonJokerPrngStream;
    public MotelySinglePrngStream RareJokerPrngStream;
}

public ref struct MotelySingleJokerFixedRarityStream
{
    public MotelyJokerRarity Rarity;
    public MotelySinglePrngStream EditionPrngStream;
    public MotelySinglePrngStream EternalPerishablePrngStream;
    public MotelySinglePrngStream RentalPrngStream;
    public MotelySinglePrngStream JokerPrngStream;
}

unsafe ref partial struct MotelySingleSearchContext
{

    public MotelySingleJokerStream CreateShopJokerStream(int ante)
    {
        return CreateJokerStream(
            MotelyPrngKeys.ShopItemSource,
            MotelyPrngKeys.ShopJokerEternalPerishableSource,
            MotelyPrngKeys.ShopJokerRentalSource,
            ante
        );
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySingleJokerStream CreateJokerStream(string source, string eternalPerishableSource, string rentalSource, int ante)
    {
        string streamSuffix = source + ante;

        return new()
        {
            EditionPrngStream = CreatePrngStream(MotelyPrngKeys.JokerEdition + streamSuffix),
            RarityPrngStream = CreatePrngStream(MotelyPrngKeys.JokerRarity + ante + source),
            EternalPerishablePrngStream = Stake >= MotelyStake.Black ? CreatePrngStream(eternalPerishableSource + ante) : MotelySinglePrngStream.Invalid,
            RentalPrngStream = Stake >= MotelyStake.Gold ? CreatePrngStream(rentalSource + ante) : MotelySinglePrngStream.Invalid,
            CommonJokerPrngStream = CreatePrngStream(MotelyPrngKeys.JokerCommon + streamSuffix),
            UncommonJokerPrngStream = CreatePrngStream(MotelyPrngKeys.JokerUncommon + streamSuffix),
            RareJokerPrngStream = CreatePrngStream(MotelyPrngKeys.JokerRare + streamSuffix)
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

        if (stickerPoll > 0.7) {
            // Eternal sticker

            if ((item.Value & Motely.JokerEternalStickerNotSupportedMask) == 0) {
                // Eternal sticker is supported on this joker
                item = item.WithEternal(true);
            }

        }

        if (Stake < MotelyStake.Orange) return item;

        if (stickerPoll > 0.4 && stickerPoll <= 0.7) {
            // Perishable Sticker

            if ((item.Value & Motely.JokerPerishableStickerNotSupportedMask) == 0) {
                // Perishable sticker is supported on this joker
                item = item.WithPerishable(true);
            }
            return item;
        }

        if (Stake < MotelyStake.Gold) return item;

        Debug.Assert(!rentalStream.IsInvalid);

        stickerPoll = GetNextRandom(ref rentalStream);

        if (stickerPoll > 0.7) {
            // Rental Sticker

            // All jokers support rental stickers
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
            rarity = MotelyJokerRarity.Rare;
        else if (rarityPoll > 0.7)
            rarity = MotelyJokerRarity.Uncommon;
        else
            rarity = MotelyJokerRarity.Common;

        MotelyItemEdition edition = GetNextEdition(ref stream.EditionPrngStream, 1);

        // Get next joker
        MotelyJoker joker;

        if (rarity == MotelyJokerRarity.Rare)
        {
            joker = (MotelyJoker)((int)MotelyJokerRarity.Rare | (int)GetNextJoker<MotelyJokerRare>(ref stream.RareJokerPrngStream));
        }
        else if (rarity == MotelyJokerRarity.Uncommon)
        {
            joker = (MotelyJoker)((int)MotelyJokerRarity.Uncommon | (int)GetNextJoker<MotelyJokerUncommon>(ref stream.UncommonJokerPrngStream));
        }
        else
        {
            Debug.Assert(rarity == MotelyJokerRarity.Common);

            joker = (MotelyJoker)((int)MotelyJokerRarity.Common | (int)GetNextJoker<MotelyJokerCommon>(ref stream.CommonJokerPrngStream));
        }

        return ApplyNextStickers(new(joker, edition), ref stream.EternalPerishablePrngStream, ref stream.RentalPrngStream);
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