
using Motely;

new MotelySearchSettings()
    .WithThreadCount(14)
    .WithFilter(new NaNSeedFilterDesc("erratic"))
    // .WithFilter(new LuckyCardFilterDesc())
    .Search();