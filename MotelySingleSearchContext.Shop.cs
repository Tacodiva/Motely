
using System.Runtime.CompilerServices;

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
    public MotelySingleSpectralStream SpectralStream;

    public readonly bool DoesProvideJokers => !JokerStream.IsNull;
    public readonly bool DoesProvideTarots => !TarotStream.IsNull;
    public readonly bool DoesProvidePlanets => !PlanetStream.IsNull;
    public readonly bool DoesProvideSpectrals => !SpectralStream.IsNull;
}

[Flags]
public enum MotelyShopStreamFlags
{
    ExcludeJokers = 1 << 1,
    ExcludeTarots = 1 << 2,
    ExcludePlanets = 1 << 3,
    ExcludeSpectrals = 1 << 4
}

unsafe ref partial struct MotelySingleSearchContext
{

    private const int ShopJokerRate = 20;

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public MotelySingleShopItemStream CreateShopItemStream(int ante, MotelyShopStreamFlags flags = 0, MotelyJokerStreamFlags jokerFlags = 0)
    {

        MotelySingleShopItemStream stream = new()
        {
            ItemTypeStream = CreatePrngStream(MotelyPrngKeys.ShopItemType + ante),
            JokerStream = flags.HasFlag(MotelyShopStreamFlags.ExcludeJokers) ?
                default : CreateShopJokerStream(ante, jokerFlags),
            TarotStream = flags.HasFlag(MotelyShopStreamFlags.ExcludeTarots) ?
                default : CreateShopTarotStream(ante),
            PlanetStream = flags.HasFlag(MotelyShopStreamFlags.ExcludePlanets) ?
                default : CreateShopPlanetStream(ante),
            SpectralStream = flags.HasFlag(MotelyShopStreamFlags.ExcludeSpectrals) || Deck != MotelyDeck.Ghost ?
                default : CreateShopSpectralStream(ante),

            TarotRate = 4,
            PlanetRate = 4,
            PlayingCardRate = 0,
            SpectralRate = 0,
        };

        if (Deck == MotelyDeck.Ghost)
        {
            stream.SpectralRate = 2;
        }

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
            if (!stream.DoesProvideJokers)
                return new(MotelyItemType.JokerExcludedByStream);

            return GetNextJoker(ref stream.JokerStream);
        }

        itemTypePoll -= ShopJokerRate;

        if (itemTypePoll < stream.TarotRate)
        {
            if (!stream.DoesProvideTarots)
                return new(MotelyItemType.TarotExcludedByStream);

            return GetNextTarot(ref stream.TarotStream);
        }

        itemTypePoll -= stream.TarotRate;

        if (itemTypePoll < stream.PlanetRate)
        {
            if (!stream.DoesProvidePlanets)
                return new(MotelyItemType.PlanetExcludedByStream);

            return GetNextPlanet(ref stream.PlanetStream);
        }

        itemTypePoll -= stream.PlanetRate;

        if (itemTypePoll < stream.PlayingCardRate)
        {
            // This shop will generate a playing card
            return new(MotelyItemType.NotImplemented);
        }

        // This shop will generate a spectral card
        if (!stream.DoesProvideSpectrals)
            return new(MotelyItemType.SpectralExcludedByStream);

        return GetNextSpectral(ref stream.SpectralStream);

    }
}