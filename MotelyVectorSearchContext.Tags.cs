
namespace Motely;

public ref struct MotelyVectorTagStream(MotelyVectorPrngStream prngStream)
{
    public MotelyVectorPrngStream PrngStream = prngStream;
}


ref partial struct MotelyVectorSearchContext
{

    public MotelyVectorTagStream CreateTagStreamCached(int ante)
    {
        if (ante == 1)
        {
            throw new NotSupportedException("Ante 1 tags are not yet supported.");
        }
        return new(CreatePrngStreamCached(MotelyPrngKeys.Tags + ante));
    }

    public MotelyVectorTagStream CreateTagStream(int ante)
    {
        if (ante == 1)
        {
            throw new NotSupportedException("Ante 1 tags are not yet supported.");
        }
        return new(CreatePrngStream(MotelyPrngKeys.Tags + ante));
    }

    public VectorEnum256<MotelyTag> GetNextTag(ref MotelyVectorTagStream tagStream)
    {
        return new(GetNextRandomInt(ref tagStream.PrngStream, 0, MotelyEnum<MotelyTag>.ValueCount));
    }
}