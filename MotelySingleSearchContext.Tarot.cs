

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Motely;

public ref struct MotelySingleTarotStream(int ante, bool soulable, MotelySingleResampleStream resampleStream, MotelySinglePrngStream soulStream)
{
    public readonly bool Soulable = soulable;
    public readonly int Ante = ante;
    public MotelySingleResampleStream ResampleStream = resampleStream;
    public MotelySinglePrngStream SoulPrngStream = soulStream;
}

ref partial struct MotelySingleSearchContext
{

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleTarotStream CreateArcanaPackTarotStreamCached(int ante)
    {
        return new(ante, true,
            CreateResampleStreamCached(MotelyPrngKeys.Tarot + MotelyPrngKeys.ArcanaPack + ante),
            CreatePrngStreamCached(MotelyPrngKeys.TerrotSoul + MotelyPrngKeys.Tarot + ante)
        );
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleTarotStream CreateArcanaPackTarotStream(int ante)
    {
        return new(ante, true,
            CreateResampleStream(MotelyPrngKeys.Tarot + MotelyPrngKeys.ArcanaPack + ante),
            CreatePrngStream(MotelyPrngKeys.TerrotSoul + MotelyPrngKeys.Tarot + ante)
        );
    }

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

    public MotelyItem GetNextTarot(ref MotelySingleTarotStream tarotStream, in MotelySingleItemSet itemSet)
    {
        if (tarotStream.Soulable && !itemSet.Contains(MotelyItemType.Soul))
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
                ref GetResamplePrngStream(ref tarotStream.ResampleStream, MotelyPrngKeys.Tarot + MotelyPrngKeys.ArcanaPack + tarotStream.Ante, resampleCount),
                0, MotelyEnum<MotelyVoucher>.ValueCount
            );

            ++resampleCount;
        }
    }
}