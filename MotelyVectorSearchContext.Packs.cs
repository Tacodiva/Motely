
namespace Motely;

public struct MotelyVectorBoosterPackStream(MotelyVectorPrngStream prngStream, bool generatedFirstPack)
{
    public MotelyVectorPrngStream PrngStream = prngStream;
    public bool GeneratedFirstPack = generatedFirstPack;

    public readonly MotelySingleBoosterPackStream CreateSingleStream(int lane)
    {
        return new(PrngStream.CreateSingleStream(lane), GeneratedFirstPack);
    }
}

ref partial struct MotelyVectorSearchContext
{
    public MotelyVectorBoosterPackStream CreateBoosterPackStream(int ante, bool isCached = false)
        => CreateBoosterPackStream(ante, ante != 1, isCached);

    public MotelyVectorBoosterPackStream CreateBoosterPackStream(int ante, bool generatedFirstPack, bool isCached = false)
    {
        return new(CreatePrngStream(MotelyPrngKeys.ShopPack + ante, isCached), generatedFirstPack);
    }

    public VectorEnum256<MotelyBoosterPack> GetNextBoosterPack(ref MotelyVectorBoosterPackStream stream)
    {
        if (!stream.GeneratedFirstPack)
        {
            stream.GeneratedFirstPack = true;
            return VectorEnum256.Create(MotelyBoosterPack.Buffoon);
        }

        return MotelyWeightedPools.BoosterPacks.Choose(GetNextRandom(ref stream.PrngStream));
    }


}