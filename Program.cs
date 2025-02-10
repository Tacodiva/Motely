
using Motely;

await new MotelySearchSettings<PerkeoObservatoryFilterDesc.PerkeoObservatoryFilter>(new PerkeoObservatoryFilterDesc())
// await new MotelySearchSettings<NegativeTagFilterDesc.NegativeTagFilter>(new NegativeTagFilterDesc())
    .WithThreadCount(Environment.ProcessorCount - 2)
    // .WithThreadCount(1)
    .Start();