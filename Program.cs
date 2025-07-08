
using Motely;

// IMotelySearch search = new MotelySearchSettings<LuckyCardFilterDesc.LuckyCardFilter>(new LuckyCardFilterDesc())
// IMotelySearch search = new MotelySearchSettings<ShuffleFinderFilterDesc.ShuffleFinderFilter>(new ShuffleFinderFilterDesc())
IMotelySearch search = new MotelySearchSettings<PerkeoObservatoryFilterDesc.PerkeoObservatoryFilter>(new PerkeoObservatoryFilterDesc())
    // await new MotelySearchSettings<NegativeTagFilterDesc.NegativeTagFilter>(new NegativeTagFilterDesc())
    // .WithThreadCount(Environment.ProcessorCount - 2)
    // .WithThreadCount(1)
    // .WithStartBatchIndex(41428)

    // .WithListSearch(["811M2111"])
    // .WithProviderSearch(new MotelyRandomSeedProvider(2000000000))
    .Start();


