namespace Motely;

public ref struct MotelyFilterCreationContext
{
    private readonly ref readonly MotelySearchParameters _searchParameters;
    private readonly HashSet<int> _cachedPseudohashKeyLengths;
    public readonly IReadOnlyCollection<int> CachedPseudohashKeyLengths => _cachedPseudohashKeyLengths;
    public bool IsAdditionalFilter;

    public readonly MotelyStake Stake => _searchParameters.Stake;
    public readonly MotelyDeck Deck => _searchParameters.Deck;

    public MotelyFilterCreationContext(ref readonly MotelySearchParameters searchParameters)
    {
        _searchParameters = ref searchParameters;
        _cachedPseudohashKeyLengths = [0];
    }

    public readonly void RemoveCachedPseudoHash(int keyLength)
    {
        _cachedPseudohashKeyLengths.Remove(keyLength);
    }

    public readonly void RemoveCachedPseudoHash(string key)
    {
        RemoveCachedPseudoHash(key.Length);
    }

    public readonly void CachePseudoHash(int keyLength, bool force = false)
    {
        // We don't cache values if they are not forced and this filter is an additional filter
        if (!force && IsAdditionalFilter) return;

        _cachedPseudohashKeyLengths.Add(keyLength);
    }

    public readonly void CachePseudoHash(string key, bool force = false)
    {
        CachePseudoHash(key.Length, force);
    }

    private readonly void CacheResampleStream(string key, bool force = false)
    {
        CachePseudoHash(key, force);
        CachePseudoHash(key + MotelyPrngKeys.Resample + "X", force);
        // We don't cache resamples >= 8 because they'd use an extra digit
    }

    public readonly void CacheBoosterPackStream(int ante, bool force = false) => CachePseudoHash(MotelyPrngKeys.ShopPack + ante, force);

    public readonly void CacheTagStream(int ante, bool force = false) => CachePseudoHash(MotelyPrngKeys.Tags + ante, force);

    public readonly void CacheVoucherStream(int ante, bool force = false) => CacheResampleStream(MotelyPrngKeys.Voucher + ante, force);

    public readonly void CacheAnteFirstVoucher(int ante, bool force = false) => CacheVoucherStream(ante, force);

    private readonly void CacheTarotStream(int ante, string source, bool cacheTarot, bool cacheResample, bool cacheSoul, bool force)
    {
        if (cacheTarot)
        {
            if (cacheResample)
            {
                CacheResampleStream(MotelyPrngKeys.Tarot + source + ante, force);
            }
            else
            {
                CachePseudoHash(MotelyPrngKeys.Tarot + source + ante, force);
            }
        }

        if (cacheSoul)
        {
            CachePseudoHash(MotelyPrngKeys.TerrotSoul + MotelyPrngKeys.Tarot + ante, force);
        }
    }

    public readonly void CacheArcanaPackTarotStream(int ante, bool soulOnly = false, bool force = false)
    {
        CacheTarotStream(ante, MotelyPrngKeys.ArcanaPackItemSource, !soulOnly, true, true, force);
    }

    public readonly void CacheShopTarotStream(int ante, bool force = false)
    {
        CacheTarotStream(ante, MotelyPrngKeys.ShopItemSource, true, false, false, force);
    }

    private readonly void CachePlanetStream(int ante, string source, bool cacheResample, bool cacheBlackHole, bool force)
    {
        if (cacheResample)
        {
            CacheResampleStream(MotelyPrngKeys.Planet + source + ante, force);
        }
        else
        {
            CachePseudoHash(MotelyPrngKeys.Planet + source + ante, force);
        }

        if (cacheBlackHole)
        {
            CachePseudoHash(MotelyPrngKeys.PlanetBlackHole + MotelyPrngKeys.Planet + ante, force);
        }
    }

    public readonly void CacheCelestialPackPlanetStream(int ante, bool force = false)
    {
        CachePlanetStream(ante, MotelyPrngKeys.CelestialPackItemSource, true, true, force);
    }

    public readonly void CacheShopPlanetStream(int ante, bool force = false)
    {
        CachePlanetStream(ante, MotelyPrngKeys.ShopItemSource, true, true, force);
    }

    public readonly void CacheStandardPackStream(int ante,
        MotelyStandardCardStreamFlags flags = MotelyStandardCardStreamFlags.Default,
        bool force = false
    )
    {
        CachePseudoHash(MotelyPrngKeys.StandardCardBase + MotelyPrngKeys.StandardPackItemSource + ante, force);

        if (!flags.HasFlag(MotelyStandardCardStreamFlags.ExcludeEnhancement))
        {
            CachePseudoHash(MotelyPrngKeys.StandardCardHasEnhancement + ante, force);
            CachePseudoHash(MotelyPrngKeys.StandardCardEnhancement + MotelyPrngKeys.StandardPackItemSource + ante, force);
        }

        if (!flags.HasFlag(MotelyStandardCardStreamFlags.ExcludeEdition))
        {
            CachePseudoHash(MotelyPrngKeys.StandardCardEdition + ante, force);
        }

        if (!flags.HasFlag(MotelyStandardCardStreamFlags.ExcludeSeal))
        {
            CachePseudoHash(MotelyPrngKeys.StandardCardHasSeal + ante, force);
            CachePseudoHash(MotelyPrngKeys.StandardCardSeal + ante, force);
        }
    }

    private readonly void CacheJokerStream(int ante,
        string source, string eternalPerishableSource, string rentalSource,
        MotelyJokerStreamFlags flags, bool force)
    {
        if (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeEdition))
        {
            CachePseudoHash(MotelyPrngKeys.JokerEdition + source + ante, force);
        }

        if (!flags.HasFlag(MotelyJokerStreamFlags.ExcludeStickers))
        {
            if (Stake >= MotelyStake.Black)
            {
                CachePseudoHash(eternalPerishableSource + ante, force);

                if (Stake >= MotelyStake.Gold)
                {
                    CachePseudoHash(rentalSource + ante, force);
                }
            }
        }
    }

    public readonly void CacheShopJokerStream(
        int ante,
        MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default,
        bool force = false
    )
    {
        CachePseudoHash(MotelyPrngKeys.JokerRarity + MotelyPrngKeys.ShopItemSource + ante, force);

        CacheJokerStream(ante,
            MotelyPrngKeys.ShopItemSource,
            MotelyPrngKeys.DefaultJokerEternalPerishableSource,
            MotelyPrngKeys.DefaultJokerRentalSource,
            flags, force
        );

        // TODO Cache the common joker stream?
    }

    private readonly void CacheFixedRarityJokerStream(int ante,
        string source, string eternalPerishableSource, string rentalSource,
        MotelyJokerRarity rarity,
        MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default,
        bool force = false
    )
    {
        CachePseudoHash(MotelyPrngKeys.FixedRarityJoker(rarity, source, ante), force);

        CacheJokerStream(ante,
            source, eternalPerishableSource, rentalSource, flags, force
        );
    }


    public readonly void CacheSoulJokerStream(
        int ante,
        MotelyJokerStreamFlags flags = MotelyJokerStreamFlags.Default,
        bool force = false
    )
    {
        CacheFixedRarityJokerStream(ante,
            MotelyPrngKeys.JokerSoulSource,
            MotelyPrngKeys.DefaultJokerEternalPerishableSource,
            MotelyPrngKeys.DefaultJokerRentalSource,
            MotelyJokerRarity.Legendary,
            flags, force
        );
    }

    public readonly void CacheShopStream(int ante,
        MotelyShopStreamFlags shopFlags = MotelyShopStreamFlags.Default,
        MotelyJokerStreamFlags jokerFlags = MotelyJokerStreamFlags.Default,
        bool force = false
    )
    {
        CachePseudoHash(MotelyPrngKeys.ShopItemType + ante);

        if (!shopFlags.HasFlag(MotelyShopStreamFlags.ExcludeJokers))
        {
            CacheShopJokerStream(ante, jokerFlags, force);
        }

        if (!shopFlags.HasFlag(MotelyShopStreamFlags.ExcludeTarots))
        {
            CacheShopTarotStream(ante, force);
        }

        if (!shopFlags.HasFlag(MotelyShopStreamFlags.ExcludePlanets))
        {
            CacheShopPlanetStream(ante, force);
        }
    }
}
