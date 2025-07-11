
using Motely;

IMotelySearch search = new MotelySearchSettings<LuckyCardFilterDesc.LuckyCardFilter>(new LuckyCardFilterDesc())
// IMotelySearch search = new MotelySearchSettings<ShuffleFinderFilterDesc.ShuffleFinderFilter>(new ShuffleFinderFilterDesc())
// IMotelySearch search = new MotelySearchSettings<PerkeoObservatoryFilterDesc.PerkeoObservatoryFilter>(new PerkeoObservatoryFilterDesc())
// IMotelySearch search = new MotelySearchSettings<NegativeTagFilterDesc.NegativeTagFilter>(new NegativeTagFilterDesc())
    .WithThreadCount(Environment.ProcessorCount - 2)
    // .WithThreadCount(1)
    // .WithStartBatchIndex(41428)

    // .WithListSearch(["ES6B2111"])
    // .WithProviderSearch(new MotelyRandomSeedProvider(2000000000))
    // .WithAdditionalFilter(new LuckyCardFilterDesc())
    .WithAdditionalFilter(new PerkeoObservatoryFilterDesc())
    .Start();


