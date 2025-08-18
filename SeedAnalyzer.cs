using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Motely
{
    /// <summary>
    /// Consolidated seed analyzer that captures seed data and provides various output formats
    /// </summary>
    public static class SeedAnalyzer
    {
        /// <summary>
        /// Analyzes a seed and returns structured data
        /// </summary>
        public static SeedAnalysisData Analyze(string seed, MotelyDeck deck, MotelyStake stake)
        {
            var data = new SeedAnalysisData 
            { 
                Seed = seed, 
                Deck = deck, 
                Stake = stake 
            };

            try
            {
                var filterDesc = new AnalyzerFilterDesc(data);
                var searchSettings = new MotelySearchSettings<AnalyzerFilterDesc.AnalyzerFilter>(filterDesc)
                    .WithDeck(deck)
                    .WithStake(stake)
                    .WithListSearch(new[] { seed })
                    .WithThreadCount(1);
                    
                var search = searchSettings.Start();
                
                // Wait for completion
                while (search.Status == MotelySearchStatus.Running)
                {
                    System.Threading.Thread.Sleep(10);
                }
                
                search.Dispose();
            }
            catch (Exception ex)
            {
                data.Error = ex.Message;
            }

            return data;
        }

        /// <summary>
        /// Analyzes a seed and prints to console
        /// </summary>
        public static void AnalyzeToConsole(string seed, MotelyDeck deck, MotelyStake stake)
        {
            var data = Analyze(seed, deck, stake);
            ConsoleFormatter.Print(data);
        }

        /// <summary>
        /// Alias for UI compatibility - captures analysis data for a seed
        /// </summary>
        public static List<SeedAnalysisData.AnteData> CaptureAnalysis(string seed, MotelyDeck deck, MotelyStake stake)
        {
            var data = Analyze(seed, deck, stake);
            return data.Antes;
        }
    }

    /// <summary>
    /// Alias for UI compatibility
    /// </summary>
    public static class SeedAnalyzerCapture
    {
        /// <summary>
        /// Captures analysis data for a seed
        /// </summary>
        public static List<AnteData> CaptureAnalysis(string seed, MotelyDeck deck, MotelyStake stake)
        {
            var data = SeedAnalyzer.Analyze(seed, deck, stake);
            return data.Antes.Select(a => new AnteData(a)).ToList();
        }

        // Wrapper classes for UI compatibility that delegate to the actual data classes
        public class AnteData(SeedAnalysisData.AnteData data)
        {
            private readonly SeedAnalysisData.AnteData _data = data;
            
            public int Ante => _data.Ante;
            public MotelyBossBlind Boss => _data.Boss;
            public MotelyVoucher Voucher => _data.Voucher;
            public List<MotelyTag> Tags => _data.Tags;
            public List<ShopItem> ShopQueue => _data.ShopQueue.Select(s => new ShopItem(s)).ToList();
            public List<PackContent> Packs => _data.Packs.Select(p => new PackContent(p)).ToList();
        }
        
        public class ShopItem(SeedAnalysisData.ShopItem data)
        {
            private readonly SeedAnalysisData.ShopItem _data = data;
            
            public int Slot => _data.Slot;
            public MotelyItem Item => _data.Item;
            public string FormattedName => _data.FormattedName;
        }
        
        public class PackContent(SeedAnalysisData.PackContent data)
        {
            private readonly SeedAnalysisData.PackContent _data = data;
            
            public MotelyBoosterPack PackType => _data.PackType;
            public MotelyBoosterPackSize PackSize => _data.PackSize;
            public List<MotelyItem> Cards => _data.Cards;
            public string FormattedName => _data.FormattedName;
            public List<string> FormattedContents => _data.FormattedContents;
            
            // Add the missing "Contents" property that the UI expects
            public List<MotelyItem> Contents => _data.Cards;
        }
    }

    /// <summary>
    /// Contains all analysis data for a seed
    /// </summary>
    public class SeedAnalysisData
    {
        public string Seed { get; set; } = "";
        public MotelyDeck Deck { get; set; }
        public MotelyStake Stake { get; set; }
        public string? Error { get; set; }
        public List<AnteData> Antes { get; set; } = new();

        public class AnteData
        {
            public int Ante { get; set; }
            public MotelyBossBlind Boss { get; set; }
            public MotelyVoucher Voucher { get; set; }
            public List<MotelyTag> Tags { get; set; } = new();
            public List<ShopItem> ShopQueue { get; set; } = new();
            public List<PackContent> Packs { get; set; } = new();
        }

        public class ShopItem
        {
            public int Slot { get; set; }
            public MotelyItem Item { get; set; }
            public string FormattedName => FormatUtils.FormatItem(Item);
        }

        public class PackContent
        {
            public MotelyBoosterPack PackType { get; set; }
            public MotelyBoosterPackSize PackSize { get; set; }
            public List<MotelyItem> Cards { get; set; } = new();
            
            public string FormattedName => FormatUtils.FormatPackName(PackType);
            public List<string> FormattedContents => Cards.ConvertAll(c => FormatUtils.FormatItem(c));
        }
    }

    /// <summary>
    /// Console output formatter for seed analysis
    /// </summary>
    public static class ConsoleFormatter
    {
        public static void Print(SeedAnalysisData data)
        {
            if (!string.IsNullOrEmpty(data.Error))
            {
                Console.WriteLine($"❌ Error analyzing seed: {data.Error}");
                return;
            }

            // Match TheSoul's format exactly
            foreach (var ante in data.Antes)
            {
                Console.WriteLine($"==ANTE {ante.Ante}==");
                Console.WriteLine($"Boss: {FormatUtils.FormatBoss(ante.Boss)}");
                Console.WriteLine($"Voucher: {FormatUtils.FormatVoucher(ante.Voucher)}");

                // Tags
                var tagNames = ante.Tags.ConvertAll(t => FormatUtils.FormatTag(t));
                Console.WriteLine($"Tags: {string.Join(", ", tagNames)}");
                
                // Shop Queue - match TheSoul format exactly: "Shop Queue: " on its own line, then numbered items
                Console.WriteLine("Shop Queue: ");
                foreach (var item in ante.ShopQueue)
                {
                    Console.WriteLine($"{item.Slot}) {item.FormattedName}");
                }
                Console.WriteLine();
                
                // Packs - match Immolate format exactly: "Pack Name - Card1, Card2, Card3"
                Console.WriteLine("Packs: ");
                foreach (var pack in ante.Packs)
                {
                    // Format: "Pack Name - Card1, Card2, Card3"
                    var contents = pack.FormattedContents.Count > 0 
                        ? " - " + string.Join(", ", pack.FormattedContents)
                        : "";
                    Console.WriteLine($"{pack.FormattedName}{contents}");
                }
                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// Shared formatting utilities
    /// </summary>
    public static class FormatUtils
    {
        public static string FormatItem(MotelyItem item)
        {
            var result = new StringBuilder();
            
            // Add seal for playing cards (BEFORE edition)
            if (item.Seal != MotelyItemSeal.None)
            {
                result.Append(item.Seal.ToString().Replace("Seal", "")).Append(" Seal ");
            }

            // Add edition if present (AFTER seal)
            if (item.Edition != MotelyItemEdition.None)
            {
                result.Append(item.Edition).Append(" ");
            }
            
            // Add enhancement for playing cards
            if (item.Enhancement != MotelyItemEnhancement.None)
            {
                result.Append(item.Enhancement).Append(" ");
            }
            
            // Format based on type
            switch (item.TypeCategory)
            {
                case MotelyItemTypeCategory.PlayingCard:
                    var playingCard = (MotelyPlayingCard)(item.Value & Motely.ItemTypeMask & ~Motely.ItemTypeCategoryMask);
                    result.Append(FormatPlayingCard(playingCard));
                    break;
                    
                default:
                    // For all other types, just use the Type enum value and format it
                    result.Append(FormatDisplayName(item.Type.ToString()));
                    break;
            }
            
            return result.ToString().Trim();
        }

        public static string FormatBoss(MotelyBossBlind boss)
        {
            return FormatDisplayName(boss.ToString());
        }

        public static string FormatVoucher(MotelyVoucher voucher)
        {
            return FormatDisplayName(voucher.ToString());
        }

        public static string FormatTag(MotelyTag tag)
        {
            var name = tag.ToString();
            // Special case for TopupTag
            if (name == "TopupTag")
                return "Top-up Tag";
            if (name.EndsWith("Tag"))
                name = name.Substring(0, name.Length - 3) + " Tag";
            return name;
        }

        // Copy of FormatDisplayName from BalatroData.cs
        public static string FormatDisplayName(string enumName)
        {
            // Special cases that need custom formatting - copied from BalatroData.cs
            var specialCases = new Dictionary<string, string>
            {
                // Numbers
                { "EightBall", "8 Ball" },
                { "Cloud9", "Cloud 9" },
                { "OopsAll6s", "Oops! All 6s" },
                // Multi-word special formatting
                { "ToTheMoon", "To the Moon" },
                { "ToDoList", "To Do List" },
                { "RiffRaff", "Riff-raff" },
                { "MailInRebate", "Mail In Rebate" },
                { "TheWheel", "The Wheel" },
                { "TheWheelOfFortune", "The Wheel of Fortune" },
                { "SockandBuskin", "Sock and Buskin" },
                { "SockAndBuskin", "Sock and Buskin" },
                { "DriversLicense", "Driver's License" },
                { "DirectorsCut", "Director's Cut" },
                { "PlanetX", "Planet X" },
                // Spectral cards
                { "Soul", "The Soul" },
                // Other special formatting
                { "MrBones", "Mr. Bones" },
                { "ChaostheClown", "Chaos the Clown" },
                // But Immolate test data has these variations we need to match:
                { "ChaosTheClown", "Chaosthe Clown" }, // Weird but matches verified output
                { "ShootTheMoon", "Shoot the Moon" },
                { "RideTheBus", "Ride the Bus" },
                { "HitTheRoad", "Hit the Road" },
                { "TheVerdant", "Verdant Leaf" },
                { "VerdantLeaf", "Verdant Leaf" },
                { "VioletVessel", "Violet Vessel" },
                { "CrimsonHeart", "Crimson Heart" },
                { "AmberAcorn", "Amber Acorn" },
                { "CeruleanBell", "Cerulean Bell" },
                {"TheFool", "The Fool" },
                {"TheMagician", "The Magician" },
                {"TheHighPriestess", "The High Priestess" },
                {"TheEmpress", "The Empress" },
                {"TheEmperor", "The Emperor" },
                {"TheHierophant", "The Hierophant" },
                {"TheLovers", "The Lovers" },
                {"TheChariot", "The Chariot" },
                {"TheHermit", "The Hermit" },
                {"Justice", "Justice" },
                {"TheJustice", "Justice" },
                {"TheHangedMan", "The Hanged Man" },
                {"Death", "Death" },
                {"TheDeath", "Death" },
                {"Temperance", "Temperance" },
                {"TheTemperance", "Temperance" },
                {"TheDevil", "The Devil" },
                {"TheTower", "The Tower" },
                {"TheStar", "The Star" },
                {"TheMoon", "The Moon" },
                {"TheSun", "The Sun" },
                {"Judgement", "Judgement" },
                {"TheJudgement", "Judgement" },
                {"TheWorld", "The World" },
                {"Strength", "Strength" }
            };

            if (specialCases.TryGetValue(enumName, out var special))
            {
                return special;
            }

            // Add spaces before capital letters (except the first one) - from BalatroData.cs
            var result = string.Empty;
            for (int i = 0; i < enumName.Length; i++)
            {
                if (i > 0 && char.IsUpper(enumName[i]) && !char.IsUpper(enumName[i - 1]))
                {
                    result += " ";
                }
                result += enumName[i];
            }

            return result;
        }

        public static string FormatJokerName(MotelyJoker joker)
        {
            var name = joker.ToString();
            return FormatDisplayName(name);
        }

        public static string FormatTarotName(string name)
        {
            // This method is now obsolete - just use FormatDisplayName
            return FormatDisplayName(name);
        }

        public static string FormatPlayingCardSuit(string suitAbbreviation)
        {
            return suitAbbreviation switch
            {
                "C" => "Clubs",
                "D" => "Diamonds",
                "H" => "Hearts",
                "S" => "Spades",
                _ => "Unknown"
            };
        }

        public static string FormatPlayingCardRank(string rankAbbreviation)
        {
            return rankAbbreviation switch
            {
                "2" => "2",
                "3" => "3",
                "4" => "4",
                "5" => "5",
                "6" => "6",
                "7" => "7",
                "8" => "8",
                "9" => "9",
                "10" => "10",
                "J" => "Jack",
                "Q" => "Queen",
                "K" => "King",
                "A" => "Ace",
                _ => throw new ArgumentOutOfRangeException(nameof(rankAbbreviation), $"Invalid rank abbreviation: {rankAbbreviation}")
            };
        }

        public static string FormatPlayingCard(MotelyPlayingCard card)
        {
            var cardStr = card.ToString();
            if (cardStr.Length >= 2)
            {
                var suit = FormatPlayingCardSuit(cardStr[0].ToString());
                var rank = cardStr.Substring(1) switch
                {
                    "2" => "2",
                    "3" => "3",
                    "4" => "4",
                    "5" => "5",
                    "6" => "6",
                    "7" => "7",
                    "8" => "8",
                    "9" => "9",
                    "10" => "10",
                    "J" => "Jack",
                    "Q" => "Queen",
                    "K" => "King",
                    "A" => "Ace",
                    _ => cardStr.Substring(1)
                };

                return $"{rank} of {suit}";
            }
            return cardStr;
        }

        public static string FormatPackName(MotelyBoosterPack pack)
        {
            return pack switch
            {
                MotelyBoosterPack.Arcana => "Arcana Pack",
                MotelyBoosterPack.JumboArcana => "Jumbo Arcana Pack",
                MotelyBoosterPack.MegaArcana => "Mega Arcana Pack",
                MotelyBoosterPack.Celestial => "Celestial Pack",
                MotelyBoosterPack.JumboCelestial => "Jumbo Celestial Pack",
                MotelyBoosterPack.MegaCelestial => "Mega Celestial Pack",
                MotelyBoosterPack.Spectral => "Spectral Pack",
                MotelyBoosterPack.JumboSpectral => "Jumbo Spectral Pack",
                MotelyBoosterPack.MegaSpectral => "Mega Spectral Pack",
                MotelyBoosterPack.Buffoon => "Buffoon Pack",
                MotelyBoosterPack.JumboBuffoon => "Jumbo Buffoon Pack",
                MotelyBoosterPack.MegaBuffoon => "Mega Buffoon Pack",
                MotelyBoosterPack.Standard => "Standard Pack",
                MotelyBoosterPack.JumboStandard => "Jumbo Standard Pack",
                MotelyBoosterPack.MegaStandard => "Mega Standard Pack",
                _ => pack.ToString()
            };
        }
    }

    /// <summary>
    /// Filter descriptor for seed analysis
    /// </summary>
    public readonly struct AnalyzerFilterDesc(SeedAnalysisData data) : IMotelySeedFilterDesc<AnalyzerFilterDesc.AnalyzerFilter>
    {
        private readonly SeedAnalysisData _data = data;

        public readonly AnalyzerFilter CreateFilter(ref MotelyFilterCreationContext ctx)
        {
            _ = ctx; // Required by interface
            return new AnalyzerFilter(_data);
        }

        public readonly struct AnalyzerFilter(SeedAnalysisData data) : IMotelySeedFilter
        {
            private readonly SeedAnalysisData _data = data;

            public readonly VectorMask Filter(ref MotelyVectorSearchContext ctx)
            {
                return ctx.SearchIndividualSeeds(CheckSeed);
            }

            public readonly bool CheckSeed(ref MotelySingleSearchContext ctx)
            {
                // Create voucher state to track activated vouchers across antes
                MotelyRunState voucherState = new();
                
                // Analyze each ante
                for (int ante = 1; ante <= 8; ante++)
                {
                    var anteData = new SeedAnalysisData.AnteData { Ante = ante };

                    // Track streams for pack contents to maintain state between packs of same type
                    var arcanaStream = default(MotelySingleTarotStream);
                    var celestialStream = default(MotelySinglePlanetStream);
                    var spectralStream = default(MotelySingleSpectralStream);
                    var standardStream = default(MotelySingleStandardCardStream);
                    var buffoonStream = default(MotelySingleJokerStream);
                    bool arcanaStreamInit = false;
                    bool celestialStreamInit = false;
                    bool spectralStreamInit = false;
                    bool standardStreamInit = false;
                    bool buffoonStreamInit = false;

                    // Boss
                    anteData.Boss = ctx.GetBossForAnte(ante);

                    // Voucher - get with state for proper progression
                    anteData.Voucher = ctx.GetAnteFirstVoucher(ante, voucherState);
                    
                    // TEST: Activate ALL vouchers from ante 1 onwards
                    voucherState.ActivateVoucher(anteData.Voucher);

                    // Tags
                    var tagStream = ctx.CreateTagStream(ante);
                    var smallTag = ctx.GetNextTag(ref tagStream);
                    var bigTag = ctx.GetNextTag(ref tagStream);
                    if (smallTag != 0) anteData.Tags.Add(smallTag);
                    if (bigTag != 0) anteData.Tags.Add(bigTag);

                    // Shop Queue
                    var shopStream = ctx.CreateShopItemStream(ante);
                    int maxSlots = ante == 1 ? 15 : 50;
                    for (int i = 0; i < maxSlots; i++)
                    {
                        var item = ctx.GetNextShopItem(ref shopStream);
                        anteData.ShopQueue.Add(new SeedAnalysisData.ShopItem
                        {
                            Slot = i + 1,
                            Item = item
                        });
                    }

                    // Packs - Get the actual shop packs (not tag-generated ones)
                    // Balatro generates 2 base shop packs, then tags can add more up to 4 in ante 1 or 6 in other antes
                    var packStream = ctx.CreateBoosterPackStream(ante, isCached: false);
                    int maxPacks = ante == 1 ? 4 : 6;
                    
                    // Get all packs up to the maximum
                    for (int i = 0; i < maxPacks; i++)
                    {
                        var pack = ctx.GetNextBoosterPack(ref packStream);
                        var packContent = ExtractPackContents(ref ctx, ante, pack, i,
                            ref arcanaStream, ref arcanaStreamInit,
                            ref celestialStream, ref celestialStreamInit,
                            ref spectralStream, ref spectralStreamInit,
                            ref standardStream, ref standardStreamInit,
                            ref buffoonStream, ref buffoonStreamInit);
                        if (packContent != null)
                        {
                            anteData.Packs.Add(packContent);
                        }
                    }

                    _data.Antes.Add(anteData);
                }

                return false; // Always return false since we're just analyzing
            }

            private static bool ShouldActivateVoucher(MotelyVoucher voucher)
            {
                // Match Immolate's banned vouchers list
                // Magic Trick should be activated (it upgrades to Illusion)
                return voucher != MotelyVoucher.Illusion &&
                       voucher != MotelyVoucher.TarotTycoon &&
                       voucher != MotelyVoucher.TarotMerchant &&
                       voucher != MotelyVoucher.PlanetTycoon &&
                       voucher != MotelyVoucher.PlanetMerchant;
            }
            
    
            private static SeedAnalysisData.PackContent ExtractPackContents(
                ref MotelySingleSearchContext ctx, int ante, MotelyBoosterPack pack, int packSlot,
                ref MotelySingleTarotStream arcanaStream, ref bool arcanaStreamInit,
                ref MotelySinglePlanetStream celestialStream, ref bool celestialStreamInit,
                ref MotelySingleSpectralStream spectralStream, ref bool spectralStreamInit,
                ref MotelySingleStandardCardStream standardStream, ref bool standardStreamInit,
                ref MotelySingleJokerStream buffoonStream, ref bool buffoonStreamInit)
            {
                var packType = pack.GetPackType();
                var packSize = pack.GetPackSize();
                var packContent = new SeedAnalysisData.PackContent
                {
                    PackType = pack,
                    PackSize = packSize
                };

                switch (packType)
                {
                    case MotelyBoosterPackType.Arcana:
                        if (!arcanaStreamInit)
                        {
                            arcanaStream = ctx.CreateArcanaPackTarotStream(ante, isCached: false);
                            arcanaStreamInit = true;
                        }
                        int arcanaCount = packType.GetCardCount(packSize);
                        var arcanaItemSet = new MotelySingleItemSet();
                        for (int i = 0; i < arcanaCount; i++)
                        {
                            var tarot = ctx.GetNextTarot(ref arcanaStream, arcanaItemSet);
                            packContent.Cards.Add(tarot);
                            arcanaItemSet.Append(tarot);
                        }
                        break;

                    case MotelyBoosterPackType.Celestial:
                        if (!celestialStreamInit)
                        {
                            celestialStream = ctx.CreateCelestialPackPlanetStream(ante);
                            celestialStreamInit = true;
                        }
                        int celestialCount = packType.GetCardCount(packSize);
                        var celestialItemSet = new MotelySingleItemSet();
                        for (int i = 0; i < celestialCount; i++)
                        {
                            var planet = ctx.GetNextPlanet(ref celestialStream, celestialItemSet);
                            packContent.Cards.Add(planet);
                            celestialItemSet.Append(planet);
                        }
                        break;

                    case MotelyBoosterPackType.Spectral:
                        if (!spectralStreamInit)
                        {
                            spectralStream = ctx.CreateSpectralPackSpectralStream(ante);
                            spectralStreamInit = true;
                        }
                        int spectralCount = packType.GetCardCount(packSize);
                        var spectralItemSet = new MotelySingleItemSet();
                        for (int i = 0; i < spectralCount; i++)
                        {
                            var spectral = ctx.GetNextSpectral(ref spectralStream, spectralItemSet);
                            packContent.Cards.Add(spectral);
                            spectralItemSet.Append(spectral);
                        }
                        break;

                    case MotelyBoosterPackType.Buffoon:
                        if (!buffoonStreamInit)
                        {
                            buffoonStream = ctx.CreateBuffoonPackJokerStream(ante, 0);
                            buffoonStreamInit = true;
                        }
                        int buffoonCount = packType.GetCardCount(packSize);
                        for (int i = 0; i < buffoonCount; i++)
                        {
                            packContent.Cards.Add(ctx.GetNextJoker(ref buffoonStream));
                        }
                        break;

                    case MotelyBoosterPackType.Standard:
                        if (!standardStreamInit)
                        {
                            standardStream = ctx.CreateStandardPackCardStream(ante);
                            standardStreamInit = true;
                        }
                        int standardCount = packType.GetCardCount(packSize);
                        for (int i = 0; i < standardCount; i++)
                        {
                            packContent.Cards.Add(ctx.GetNextStandardCard(ref standardStream));
                        }
                        break;
                }

                return packContent;
            }
        }
    }
}