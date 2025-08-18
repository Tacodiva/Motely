
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public ref struct MotelyVectorTagStream(MotelyVectorResampleStream resampleStream, int ante)
{
    public int Ante = ante;
    public MotelyVectorResampleStream ResampleStream = resampleStream;
}


ref partial struct MotelyVectorSearchContext
{

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyVectorTagStream CreateTagStream(int ante, bool isCached = false)
    {
        return new(CreateResampleStream(MotelyPrngKeys.Tags + ante, isCached), ante);
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorEnum256<MotelyTag> GetNextTag(ref MotelyVectorTagStream tagStream)
    {
        if (tagStream.Ante > 1)
        {
            return new(GetNextRandomInt(ref tagStream.ResampleStream.InitialPrngStream, 0, MotelyEnum<MotelyTag>.ValueCount));
        }

        VectorEnum256<MotelyTag> tags = new(
            GetNextRandomInt(ref tagStream.ResampleStream.InitialPrngStream, 0, MotelyEnum<MotelyTag>.ValueCount)
        );

        int resampleCount = 0;

        while (true)
        {
            Vector256<int> resampleMask = Vector256<int>.Zero;

            resampleMask |= VectorEnum256.Equals(tags, MotelyTag.NegativeTag);
            resampleMask |= VectorEnum256.Equals(tags, MotelyTag.StandardTag);
            resampleMask |= VectorEnum256.Equals(tags, MotelyTag.MeteorTag);
            resampleMask |= VectorEnum256.Equals(tags, MotelyTag.BuffoonTag);
            resampleMask |= VectorEnum256.Equals(tags, MotelyTag.HandyTag);
            resampleMask |= VectorEnum256.Equals(tags, MotelyTag.GarbageTag);
            resampleMask |= VectorEnum256.Equals(tags, MotelyTag.EtherealTag);
            resampleMask |= VectorEnum256.Equals(tags, MotelyTag.TopupTag);
            resampleMask |= VectorEnum256.Equals(tags, MotelyTag.OrbitalTag);

            if (Vector256.EqualsAll(resampleMask, Vector256<int>.Zero))
                break;

            Vector256<int> newTags = GetNextRandomInt(
                ref GetResamplePrngStream(ref tagStream.ResampleStream, MotelyPrngKeys.Tags + tagStream.Ante, resampleCount),
                0, MotelyEnum<MotelyTag>.ValueCount,
                MotelyVectorUtils.ExtendIntMaskToDouble(resampleMask)
            );

            tags = new(Vector256.ConditionalSelect(resampleMask, newTags, tags.HardwareVector));

            ++resampleCount;
        }

        return tags;
    }
}