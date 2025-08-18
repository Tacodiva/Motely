
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public ref struct MotelySingleTagStream(MotelySingleResampleStream resampleStream, int ante)
{
    public int Ante = ante;
    public MotelySingleResampleStream ResampleStream = resampleStream;
}


ref partial struct MotelySingleSearchContext
{

    private static readonly MotelyTag[] DisallowedAnteOneTags = [
        MotelyTag.NegativeTag,
        MotelyTag.StandardTag,
        MotelyTag.MeteorTag,
        MotelyTag.BuffoonTag,
        MotelyTag.HandyTag,
        MotelyTag.GarbageTag,
        MotelyTag.EtherealTag,
        MotelyTag.TopupTag,
        MotelyTag.OrbitalTag
    ];


#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleTagStream CreateTagStream(int ante, bool isCached = false)
    {
        return new(CreateResampleStream(MotelyPrngKeys.Tags + ante, isCached), ante);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyTag GetNextTag(ref MotelySingleTagStream tagStream)
    {
        if (tagStream.Ante > 1)
        {
            return (MotelyTag)GetNextRandomInt(ref tagStream.ResampleStream.InitialPrngStream, 0, MotelyEnum<MotelyTag>.ValueCount);
        }

        MotelyTag tag = (MotelyTag)
            GetNextRandomInt(ref tagStream.ResampleStream.InitialPrngStream, 0, MotelyEnum<MotelyTag>.ValueCount);

        int resampleCount = 0;

        while (DisallowedAnteOneTags.Contains(tag))
        {
            tag = (MotelyTag) GetNextRandomInt(
                ref GetResamplePrngStream(ref tagStream.ResampleStream, MotelyPrngKeys.Tags + tagStream.Ante, resampleCount),
                0, MotelyEnum<MotelyTag>.ValueCount
            );

            ++resampleCount;
        }

        return tag;
    }
}