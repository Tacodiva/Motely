
using Motely;

await new MotelySearchSettings<PerkeoObservatoryFilterDesc.PerkeoObservatoryFilter>(new PerkeoObservatoryFilterDesc())
    .WithThreadCount(Environment.ProcessorCount - 2)
    // .WithThreadCount(1)
    .Start();