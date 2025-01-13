
using Motely;

new MotelySearchSettings<NaNSeedFilterDesc, NaNSeedFilterDesc.NaNSeedFilter>(new NaNSeedFilterDesc("erratic"))
// new MotelySearchSettings<LuckyCardFilterDesc, LuckyCardFilterDesc.LuckyCardFilter>(new LuckyCardFilterDesc())
    .WithThreadCount(Environment.ProcessorCount - 2)
    .Search();