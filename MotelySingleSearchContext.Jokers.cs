
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Motely;

public ref struct MotelySingleJokerFixedRarityStream
{
    public int Ante;
    public MotelyJokerRarity Rarity;
    public MotelySinglePrngStream EditionPrngStream;
    public MotelySinglePrngStream JokerStream;
}

unsafe ref partial struct MotelySingleSearchContext
{

    public MotelySingleJokerFixedRarityStream CreateSoulJokerStream(int ante)
    {
        return CreateJokerFixedRarityStream(MotelyPrngKeys.JokerSoul, ante, MotelyJokerRarity.Legendary);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelySingleJokerFixedRarityStream CreateJokerFixedRarityStream(string source, int ante, MotelyJokerRarity rarity)
    {
        MotelySingleJokerFixedRarityStream stream = new()
        {
            Ante = ante,
            Rarity = rarity,
            EditionPrngStream = CreatePrngStream(MotelyPrngKeys.JokerEdition + source + ante),
        };

        switch (rarity)
        {
            case MotelyJokerRarity.Legendary:
                stream.JokerStream = CreatePrngStream(MotelyPrngKeys.JokerLegendary);
                break;
            default:
                throw new NotSupportedException();
        }

        return stream;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyItem NextJoker(ref MotelySingleJokerFixedRarityStream stream)
    {
        MotelyItemEdition edition;

        {
            int editionRate = 1;

            double editionPoll = GetNextRandom(ref stream.EditionPrngStream);

            if (editionPoll > 0.997)
                edition = MotelyItemEdition.Negative;
            else if (editionPoll > 1 - 0.006 * editionRate)
                edition = MotelyItemEdition.Polychrome;
            else if (editionPoll > 1 - 0.02 * editionRate)
                edition = MotelyItemEdition.Holographic;
            else if (editionPoll > 1 - 0.04 * editionRate)
                edition = MotelyItemEdition.Foil;
            else
                edition = MotelyItemEdition.None;
        }

        return NextJoker(stream.Rarity, edition, ref stream.JokerStream);
    }

    // #if !DEBUG
    //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // #endif
    //     public MotelyItem NextJoker(ref MotelySingleJokerFixedRarityStream stream)
    //     {
    //         MotelyJokerRarity rarity;

    //         switch (stream.Source)
    //         {
    //             case MotelyPrngKeys.JokerSoul:
    //                 rarity = MotelyJokerRarity.Legendary;
    //                 break;
    //             default:
    //                 double rarityPoll = GetNextRandom(ref stream.RarityPrngStream);

    //                 if (rarityPoll > 0.95)
    //                     rarity = MotelyJokerRarity.Rare;
    //                 else if (rarityPoll > 0.7)
    //                     rarity = MotelyJokerRarity.Uncommon;
    //                 else
    //                     rarity = MotelyJokerRarity.Common;
    //                 break;
    //         }

    //         MotelyItemEdition edition;

    //         {
    //             int editionRate = 1;

    //             double editionPoll = GetNextRandom(ref stream.EditionPrngStream);

    //             if (editionPoll > 0.997)
    //                 edition = MotelyItemEdition.Negative;
    //             else if (editionPoll > 1 - 0.006 * editionRate)
    //                 edition = MotelyItemEdition.Polychrome;
    //             else if (editionPoll > 1 - 0.02 * editionRate)
    //                 edition = MotelyItemEdition.Holographic;
    //             else if (editionPoll > 1 - 0.04 * editionRate)
    //                 edition = MotelyItemEdition.Foil;
    //             else
    //                 edition = MotelyItemEdition.None;
    //         }

    //         // Get next joker
    //         MotelyJoker joker;

    //         if (rarity == MotelyJokerRarity.Legendary)
    //         {
    //             joker = (MotelyJoker)((int)MotelyJokerRarity.Legendary | (int)NextJoker<MotelyJokerLegendary>(ref stream.JokerStream));
    //         }
    //         else if (rarity == MotelyJokerRarity.Rare)
    //         {
    //             joker = (MotelyJoker)((int)MotelyJokerRarity.Rare | (int)NextJoker<MotelyJokerRare>(ref stream.JokerStream));
    //         }
    //         else if (rarity == MotelyJokerRarity.Uncommon)
    //         {
    //             joker = (MotelyJoker)((int)MotelyJokerRarity.Uncommon | (int)NextJoker<MotelyJokerUncommon>(ref stream.JokerStream));
    //         }
    //         else
    //         {
    //             Debug.Assert(rarity == MotelyJokerRarity.Common);

    //             joker = (MotelyJoker)((int)MotelyJokerRarity.Common | (int)NextJoker<MotelyJokerCommon>(ref stream.JokerStream));
    //         }

    //         return new(joker, edition);
    //     }


#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private MotelyItem NextJoker(MotelyJokerRarity rarity, MotelyItemEdition edition, ref MotelySinglePrngStream stream)
    {
        MotelyJoker joker;

        switch (rarity)
        {
            case MotelyJokerRarity.Legendary:
                joker = (MotelyJoker)((int)MotelyJokerRarity.Legendary | (int)NextJoker<MotelyJokerLegendary>(ref stream));
                break;
            case MotelyJokerRarity.Rare:
                joker = (MotelyJoker)((int)MotelyJokerRarity.Rare | (int)NextJoker<MotelyJokerRare>(ref stream));
                break;
            case MotelyJokerRarity.Uncommon:
                joker = (MotelyJoker)((int)MotelyJokerRarity.Uncommon | (int)NextJoker<MotelyJokerUncommon>(ref stream));
                break;
            default:
                Debug.Assert(rarity == MotelyJokerRarity.Common);
                joker = (MotelyJoker)((int)MotelyJokerRarity.Common | (int)NextJoker<MotelyJokerCommon>(ref stream));
                break;
        }

        return new(joker, edition);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private T NextJoker<T>(ref MotelySinglePrngStream stream) where T : unmanaged, Enum
    {
        Debug.Assert(sizeof(T) == 4);
        int value = GetNextRandomInt(ref stream, 0, MotelyEnum<T>.ValueCount);
        return Unsafe.As<int, T>(ref value);
    }
}