
namespace Motely;

public ref struct MotelySingleStandardCardStream
{
    public MotelySinglePrngStream CardPrngStream;
    public MotelySinglePrngStream HasEnhancementPrngStream;
    public MotelySinglePrngStream EnhancementPrngStream;
    public MotelySinglePrngStream EditionPrngStream;
    public MotelySinglePrngStream HasSealPrngStream;
    public MotelySinglePrngStream SealPrngStream;
}

[Flags]
public enum MotelyStandardCardStreamFlags
{
    ExcludeEnhancement = 1 << 1,
    ExcludeEdition = 1 << 2,
    ExcludeSeal = 1 << 3,
}

ref partial struct MotelySingleSearchContext
{

    public MotelySingleStandardCardStream CreateStandardPackCardStream(int ante, MotelyStandardCardStreamFlags flags = 0)
    {
        return new()
        {
            CardPrngStream = CreatePrngStream(MotelyPrngKeys.StandardCardBase + MotelyPrngKeys.StandardPackItemSource + ante),

            HasEnhancementPrngStream = flags.HasFlag(MotelyStandardCardStreamFlags.ExcludeEnhancement) ?
                MotelySinglePrngStream.Invalid :
                CreatePrngStream(MotelyPrngKeys.StandardCardHasEnhancement + ante),
            EnhancementPrngStream = flags.HasFlag(MotelyStandardCardStreamFlags.ExcludeEnhancement) ?
                MotelySinglePrngStream.Invalid :
                CreatePrngStream(MotelyPrngKeys.StandardCardEnhancement + MotelyPrngKeys.StandardPackItemSource + ante),

            EditionPrngStream = flags.HasFlag(MotelyStandardCardStreamFlags.ExcludeEdition) ?
                MotelySinglePrngStream.Invalid :
                CreatePrngStream(MotelyPrngKeys.StandardCardEdition + ante),

            HasSealPrngStream = flags.HasFlag(MotelyStandardCardStreamFlags.ExcludeSeal) ?
                MotelySinglePrngStream.Invalid :
                CreatePrngStream(MotelyPrngKeys.StandardCardHasSeal + ante),
            SealPrngStream = flags.HasFlag(MotelyStandardCardStreamFlags.ExcludeSeal) ?
                MotelySinglePrngStream.Invalid :
                CreatePrngStream(MotelyPrngKeys.StandardCardSeal + ante),
        };
    }

    public MotelySingleItemSet GetNextStandardPackContents(ref MotelySingleStandardCardStream stream, MotelyBoosterPackSize size)
    {
        MotelySingleItemSet pack = new();
        int cardCount = MotelyBoosterPackType.Standard.GetCardCount(size);

        for (int i = 0; i < cardCount; i++)
            pack.Append(GetNextStandardCard(ref stream));

        return pack;

    }

    public MotelyItem GetNextStandardCard(ref MotelySingleStandardCardStream stream)
    {
        MotelyItem item = new(MotelyEnum<MotelyPlayingCard>.Values[GetNextRandomInt(ref stream.CardPrngStream, 0, MotelyEnum<MotelyPlayingCard>.ValueCount)]);

        // Enhancement
        if (!stream.HasEnhancementPrngStream.IsInvalid && GetNextRandom(ref stream.HasEnhancementPrngStream) > 0.6)
        {
            item = item.WithEnhancement(
                (MotelyItemEnhancement)(GetNextRandomInt(ref stream.EnhancementPrngStream, 1, MotelyEnum<MotelyItemEnhancement>.ValueCount) << Motely.ItemEnhancementOffset)
            );
        }

        // Edition
        if (!stream.EditionPrngStream.IsInvalid)
        {
            double editionPoll = GetNextRandom(ref stream.EditionPrngStream);
            if (editionPoll > 0.988)
                item = item.WithEdition(MotelyItemEdition.Polychrome);
            else if (editionPoll > 0.96)
                item = item.WithEdition(MotelyItemEdition.Holographic);
            else if (editionPoll > 0.92)
                item = item.WithEdition(MotelyItemEdition.Foil);
        }

        // Seal
        if (!stream.HasSealPrngStream.IsInvalid && GetNextRandom(ref stream.HasSealPrngStream) > 0.8)
        {
            double sealPoll = GetNextRandom(ref stream.SealPrngStream);

            if (sealPoll > 0.75)
                item = item.WithSeal(MotelyItemSeal.Red);
            else if (sealPoll > 0.5)
                item = item.WithSeal(MotelyItemSeal.Blue);
            else if (sealPoll > 0.25)
                item = item.WithSeal(MotelyItemSeal.Gold);
            else
                item = item.WithSeal(MotelyItemSeal.Purple);
        }

        return item;
    }
}