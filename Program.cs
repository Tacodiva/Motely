
using Motely;

// new MotelySearchSettings<NaNSeedFilterDesc, NaNSeedFilterDesc.NaNSeedFilter>(new NaNSeedFilterDesc("erratic"))
new MotelySearchSettings<LuckyCardFilterDesc, LuckyCardFilterDesc.LuckyCardFilter>(new LuckyCardFilterDesc())
    .WithThreadCount(14)
    .Search();