
namespace Motely;

public struct MotelySingleBoosterPackStream(MotelySinglePrngStream prngStream, bool generatedFirstPack)
{
    public MotelySinglePrngStream PrngStream = prngStream;
    public bool GeneratedFirstPack = generatedFirstPack;
}

ref partial struct MotelySingleSearchContext
{
    public MotelySingleBoosterPackStream CreateBoosterPackStream(int ante, bool isCached = false)
        => CreateBoosterPackStream(ante, ante != 1, isCached);

    public MotelySingleBoosterPackStream CreateBoosterPackStream(int ante, bool generatedFirstPack, bool isCached = false)
    {
        return new(CreatePrngStream(MotelyPrngKeys.ShopPack + ante, isCached), generatedFirstPack);
    }

    public MotelyBoosterPack GetNextBoosterPack(ref MotelySingleBoosterPackStream stream)
    {
        if (!stream.GeneratedFirstPack)
        {
            stream.GeneratedFirstPack = true;
            return MotelyBoosterPack.Buffoon;
        }

        return MotelyWeightedPools.BoosterPacks.Choose(GetNextRandom(ref stream.PrngStream));
    }
}