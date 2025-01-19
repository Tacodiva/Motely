
using Motely;

// new MotelySearchSettings<NaNSeedFilterDesc, NaNSeedFilterDesc.NaNSeedFilter>(new NaNSeedFilterDesc("erratic"))
// new MotelySearchSettings<LuckyCardFilterDesc, LuckyCardFilterDesc.LuckyCardFilter>(new LuckyCardFilterDesc())
new MotelySearchSettings<PerkeoObservatoryFilterDesc, PerkeoObservatoryFilterDesc.PerkeoObservatoryFilter>(new PerkeoObservatoryFilterDesc())
    // .WithThreadCount(Environment.ProcessorCount - 2)
    .WithThreadCount(1)
    .Search();