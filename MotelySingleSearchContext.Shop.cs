
namespace Motely;

public ref struct MotelySingleShopItemStream
{
    public double TarotRate;
    public double PlanetRate;
    public double PlayingCardRate;
    public double SpectralRate;
    public double TotalRate;
    public MotelySinglePrngStream ItemTypeStream;
    public MotelySingleJokerStream JokerStream;
    public MotelySingleTarotStream TarotStream;
    public MotelySinglePlanetStream PlanetStream;
}

unsafe ref partial struct MotelySingleSearchContext
{

    private const int ShopJokerRate = 20;

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleShopItemStream CreateShopItemStream(int ante)
    {

        MotelySingleShopItemStream stream = new()
        {
            ItemTypeStream = CreatePrngStream(MotelyPrngKeys.ShopItemType + ante),
            JokerStream = CreateShopJokerStream(ante),
            TarotStream = CreateShopTarotStream(ante),
            PlanetStream = CreateShopPlanetStream(ante),
            
            TarotRate = 4,
            PlanetRate = 4,
            PlayingCardRate = 0,
            SpectralRate = 0,
        };

        stream.TotalRate = ShopJokerRate + stream.TarotRate + stream.PlanetRate + stream.PlayingCardRate + stream.SpectralRate;

        return stream;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelyItem GetNextShopItem(ref MotelySingleShopItemStream stream)
    {
        double itemTypePoll = GetNextRandom(ref stream.ItemTypeStream) * stream.TotalRate;

        if (itemTypePoll < ShopJokerRate)
        {
            return GetNextJoker(ref stream.JokerStream);
        }

        itemTypePoll -= ShopJokerRate;

        if (itemTypePoll < stream.TarotRate)
        {
            return GetNextTarot(ref stream.TarotStream);
        }

        itemTypePoll -= stream.TarotRate;

        if (itemTypePoll < stream.PlanetRate)
        {
            return GetNextPlanet(ref stream.PlanetStream);
        }

        itemTypePoll -= stream.PlanetRate;

        if (itemTypePoll < stream.PlayingCardRate)
        {
            // This shop will generate a playing card
            return new(MotelyItemType.HA);
        }

        // This shop will generate a spectral card
        return new(MotelyItemType.Immolate);

    }
}