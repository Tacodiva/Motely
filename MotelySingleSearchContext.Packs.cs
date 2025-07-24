
namespace Motely;

public struct MotelySingleBoosterPackStream(MotelySinglePrngStream prngStream, bool generatedFirstPack)
{
    public MotelySinglePrngStream PrngStream = prngStream;
    public bool GeneratedFirstPack = generatedFirstPack;
}

ref partial struct MotelySingleSearchContext
{
    public MotelySingleBoosterPackStream CreateBoosterPackStreamCached(int ante)
    => CreateBoosterPackStreamCached(ante, ante != 1);

    public MotelySingleBoosterPackStream CreateBoosterPackStreamCached(int ante, bool generatedFirstPack)
    {
        return new(CreatePrngStreamCached(MotelyPrngKeys.ShopPack + ante), generatedFirstPack);
    }

    public MotelySingleBoosterPackStream CreateBoosterPackStream(int ante)
        => CreateBoosterPackStream(ante, ante != 1);

    public MotelySingleBoosterPackStream CreateBoosterPackStream(int ante, bool generatedFirstPack)
    {
        return new(CreatePrngStream(MotelyPrngKeys.ShopPack + ante), generatedFirstPack);
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