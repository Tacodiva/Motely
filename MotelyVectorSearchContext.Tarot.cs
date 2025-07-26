
namespace Motely;

public ref struct MotelyVectorTarotStream(string resampleKey, MotelyVectorResampleStream resampleStream, MotelyVectorPrngStream soulStream)
{
    public readonly bool IsNull => ResampleKey == null;
    public readonly string ResampleKey = resampleKey;
    public MotelyVectorResampleStream ResampleStream = resampleStream;
    public MotelyVectorPrngStream SoulPrngStream = soulStream;
    public readonly bool IsSoulable => !SoulPrngStream.IsInvalid;
}

ref partial struct MotelyVectorSearchContext
{
    
}