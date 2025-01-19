
namespace Motely;

public struct MotelyVectorBoosterPackStream(MotelyVectorPrngStream prngStream, bool generatedFirstPack)
{
    public MotelyVectorPrngStream PrngStream = prngStream;
    public bool GeneratedFirstPack = generatedFirstPack;
}

ref partial struct MotelyVectorSearchContext
{
    public MotelyVectorBoosterPackStream CreateBoosterPackStream(int ante)
        => CreateBoosterPackStream(ante, ante != 1);

    public MotelyVectorBoosterPackStream CreateBoosterPackStream(int ante, bool generatedFirstPack)
    {
        return new(CreatePrngStream(MotelyPrngKeys.ShopPack + ante), generatedFirstPack);
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