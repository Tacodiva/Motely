using System.ComponentModel;
using System.Diagnostics;
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
        public static MotelySeedAnalysis Analyze(MotelySeedAnalysisConfig cfg)
        {
            try
            {
                AnalyzerFilterDesc filterDesc = new();

                var searchSettings = new MotelySearchSettings<AnalyzerFilterDesc.AnalyzerFilter>(filterDesc)
                    .WithDeck(cfg.Deck)
                    .WithStake(cfg.Stake)
                    .WithListSearch([cfg.Seed])
                    .WithThreadCount(1);

                using var search = searchSettings.Start();

                search.AwaitCompletion();

                Debug.Assert(filterDesc.LastAnalysis != null);

                return filterDesc.LastAnalysis;
            }
            catch (Exception ex)
            {
                return new MotelySeedAnalysis(ex.ToString(), []);
            }
        }
    }

    public sealed record class MotelySeedAnalysisConfig
    (
        string Seed,
        MotelyDeck Deck,
        MotelyStake Stake
    );

    /// <summary>
    /// Contains all analysis data for a seed
    /// </summary>
    public sealed record class MotelySeedAnalysis
    (
        string? Error,
        IReadOnlyList<MotelyAnteAnalysis> Antes
    )
    {

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Error))
            {
                return $"❌ Error analyzing seed: {Error}";
            }

            StringBuilder sb = new();

            // Match TheSoul's format exactly
            foreach (var ante in Antes)
            {
                sb.AppendLine($"==ANTE {ante.Ante}==");
                sb.AppendLine($"Boss: {FormatUtils.FormatBoss(ante.Boss)}");
                sb.AppendLine($"Voucher: {FormatUtils.FormatVoucher(ante.Voucher)}");

                // Tags
                sb.AppendLine($"Tags: {FormatUtils.FormatTag(ante.SmallBlindTag)}, {FormatUtils.FormatTag(ante.BigBlindTag)}");

                // Shop Queue - match TheSoul format exactly: "Shop Queue: " on its own line, then numbered items
                sb.AppendLine("Shop Queue: ");
                foreach ((int i, MotelyItem item) in ante.ShopQueue.Index())
                {
                    sb.AppendLine($"{i + 1}) {FormatUtils.FormatItem(item)}");
                }
                sb.AppendLine();

                // Packs - match Immolate format exactly: "Pack Name - Card1, Card2, Card3"
                sb.AppendLine("Packs: ");
                foreach (var pack in ante.Packs)
                {
                    // Format: "Pack Name - Card1, Card2, Card3"
                    var contents = pack.Items.Count > 0
                        ? " - " + string.Join(", ", pack.Items.Select(item => FormatUtils.FormatItem(item)))
                        : "";
                    sb.AppendLine($"{FormatUtils.FormatPackName(pack.Type)}{contents}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    public sealed record class MotelyAnteAnalysis
    (
        int Ante,
        MotelyBossBlind Boss,
        MotelyVoucher Voucher,
        MotelyTag SmallBlindTag,
        MotelyTag BigBlindTag,
        IReadOnlyList<MotelyItem> ShopQueue,
        IReadOnlyList<MotelyBoosterPackAnalysis> Packs
    );

    public sealed record class MotelyBoosterPackAnalysis
    (
        MotelyBoosterPack Type,
        IReadOnlyList<MotelyItem> Items
    );
    
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
    public sealed class AnalyzerFilterDesc() : IMotelySeedFilterDesc<AnalyzerFilterDesc.AnalyzerFilter>
    {
        public MotelySeedAnalysis? LastAnalysis { get; private set; } = null;

        public AnalyzerFilter CreateFilter(ref MotelyFilterCreationContext ctx)
        {
            return new AnalyzerFilter(this);
        }

        public readonly struct AnalyzerFilter(AnalyzerFilterDesc filterDesc) : IMotelySeedFilter
        {

            public AnalyzerFilterDesc FilterDesc { get; } = filterDesc;

            public readonly VectorMask Filter(ref MotelyVectorSearchContext ctx)
            {
                return ctx.SearchIndividualSeeds(CheckSeed);
            }

            private ref struct AnteAnalysisState
            {
                public MotelySingleTarotStream ArcanaStream;
                public readonly bool HasArcanaStream => !ArcanaStream.IsNull;
                public MotelySinglePlanetStream CelestialStream;
                public readonly bool HasCelestialStream => !CelestialStream.IsNull;
                public MotelySingleSpectralStream SpectralStream;
                public readonly bool HasSpectralStream => !SpectralStream.IsNull;
                public MotelySingleStandardCardStream StandardStream;
                public readonly bool HasStandardStream => !StandardStream.IsInvalid;
                public MotelySingleJokerStream BuffoonStream;
                public readonly bool HasBuffoonStream => !BuffoonStream.IsNull;
            }

            public readonly bool CheckSeed(ref MotelySingleSearchContext ctx)
            {
                // Create voucher state to track activated vouchers across antes
                MotelyRunState voucherState = new();

                List<MotelyAnteAnalysis> antes = [];

                // Analyze each ante
                for (int ante = 1; ante <= 8; ante++)
                {

                    AnteAnalysisState state = new()
                    {
                        ArcanaStream = default,
                        CelestialStream = default,
                        SpectralStream = default,
                        StandardStream = MotelySingleStandardCardStream.Invalid,
                        BuffoonStream = default
                    };

                    // Boss
                    MotelyBossBlind boss = ctx.GetBossForAnte(ante);

                    // Voucher - get with state for proper progression
                    MotelyVoucher voucher = ctx.GetAnteFirstVoucher(ante, voucherState);

                    // TEST: Activate ALL vouchers from ante 1 onwards
                    // if (ShouldActivateVoucher(voucher))
                    voucherState.ActivateVoucher(voucher);

                    // Tags
                    MotelySingleTagStream tagStream = ctx.CreateTagStream(ante);

                    MotelyTag smallTag = ctx.GetNextTag(ref tagStream);
                    MotelyTag bigTag = ctx.GetNextTag(ref tagStream);

                    // Shop Queue
                    MotelySingleShopItemStream shopStream = ctx.CreateShopItemStream(ante);

                    int maxSlots = ante == 1 ? 15 : 50;
                    MotelyItem[] shopItems = new MotelyItem[maxSlots];

                    for (int i = 0; i < maxSlots; i++)
                    {
                        shopItems[i] = ctx.GetNextShopItem(ref shopStream);
                    }

                    // Packs - Get the actual shop packs (not tag-generated ones)
                    // Balatro generates 2 base shop packs, then tags can add more up to 4 in ante 1 or 6 in other antes
                    var packStream = ctx.CreateBoosterPackStream(ante);

                    int maxPacks = ante == 1 ? 4 : 6;
                    MotelyBoosterPackAnalysis[] packs = new MotelyBoosterPackAnalysis[maxPacks];

                    // Get all packs up to the maximum
                    for (int i = 0; i < maxPacks; i++)
                    {
                        MotelyBoosterPack pack = ctx.GetNextBoosterPack(ref packStream);
                        MotelySingleItemSet packContent = GetPackContents(ref ctx, ante, pack, ref state);

                        packs[i] = new(pack, packContent.AsArray());
                    }

                    antes.Add(new(
                        ante,
                        boss,
                        voucher,
                        smallTag,
                        bigTag,
                        shopItems,
                        packs
                    ));
                }

                FilterDesc.LastAnalysis = new(null, antes);

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


            private static MotelySingleItemSet GetPackContents(
                ref MotelySingleSearchContext ctx, int ante, MotelyBoosterPack pack, ref AnteAnalysisState state
            )
            {
                var packType = pack.GetPackType();
                var packSize = pack.GetPackSize();

                switch (packType)
                {
                    case MotelyBoosterPackType.Arcana:

                        if (!state.HasArcanaStream)
                            state.ArcanaStream = ctx.CreateArcanaPackTarotStream(ante);

                        return ctx.GetNextArcanaPackContents(ref state.ArcanaStream, packSize);

                    case MotelyBoosterPackType.Celestial:

                        if (!state.HasCelestialStream)
                            state.CelestialStream = ctx.CreateCelestialPackPlanetStream(ante);

                        return ctx.GetNextCelestialPackContents(ref state.CelestialStream, packSize);

                    case MotelyBoosterPackType.Spectral:

                        if (!state.HasSpectralStream)
                            state.SpectralStream = ctx.CreateSpectralPackSpectralStream(ante);

                        return ctx.GetNextSpectralPackContents(ref state.SpectralStream, packSize);

                    case MotelyBoosterPackType.Buffoon:

                        if (!state.HasBuffoonStream)
                            state.BuffoonStream = ctx.CreateBuffoonPackJokerStream(ante);

                        return ctx.GetNextBuffoonPackContents(ref state.BuffoonStream, packSize);

                    case MotelyBoosterPackType.Standard:

                        if (!state.HasStandardStream)
                            state.StandardStream = ctx.CreateStandardPackCardStream(ante);

                        return ctx.GetNextStandardPackContents(ref state.StandardStream, packSize);

                    default:
                        throw new InvalidEnumArgumentException();
                }
            }
        }
    }
}