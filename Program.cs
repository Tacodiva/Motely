
using Motely;

// await new MotelySearchSettings<NaNSeedFilterDesc.NaNSeedFilter>(new NaNSeedFilterDesc("erratic"))
// await new MotelySearchSettings<LuckyCardFilterDesc.LuckyCardFilter>(new LuckyCardFilterDesc())
await new MotelySearchSettings<PerkeoObservatoryFilterDesc.PerkeoObservatoryFilter>(new PerkeoObservatoryFilterDesc())
    // .WithThreadCount(Environment.ProcessorCount - 2)
    .WithThreadCount(1)
    .Start();