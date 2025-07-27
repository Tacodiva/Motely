
using Motely;



// IMotelySearch search = new MotelySearchSettings<FilledSoulFilterDesc.SoulFilter>(new FilledSoulFilterDesc())
IMotelySearch search = new MotelySearchSettings<TestFilterDesc.TestFilter>(new TestFilterDesc())
    // IMotelySearch search = new MotelySearchSettings<LuckCardFilterDesc.LuckyCardFilter>(new LuckCardFilterDesc())
    // IMotelySearch search = new MotelySearchSettings<ShuffleFinderFilterDesc.ShuffleFinderFilter>(new ShuffleFinderFilterDesc())
    // IMotelySearch search = new MotelySearchSettings<PerkeoObservatoryFilterDesc.PerkeoObservatoryFilter>(new PerkeoObservatoryFilterDesc())
    // IMotelySearch search = new MotelySearchSettings<NegativeTagFilterDesc.NegativeTagFilter>(new NegativeTagFilterDesc())
    .WithThreadCount(Environment.ProcessorCount - 2)
    // .WithThreadCount(1)
    // .WithStartBatchIndex(41428)

    // .WithProviderSearch(new MotelyRandomSeedProvider(2000000000))
    // .WithAdditionalFilter(new LuckyCardFilterDesc())
    // .WithAdditionalFilter(new PerkeoObservatoryFilterDesc())
    // .WithListSearch(["TACO", "DIVA", "7729", "AAAA", "BBBB", "CCCC", "DDDD", "EEEE"])
    // .WithListSearch(["TIQR1111"])
    // .WithStake(MotelyStake.Black)
    .Start();


