using System.Text;

namespace Motely;

/// <summary>
/// Shared formatting utilities
/// </summary>
internal static class FormatUtils
{
    public static string FormatItem(MotelyItem item)
    {
        var result = new StringBuilder();

        // Add stickers (Eternal, Perishable, Rental) FIRST
        if (item.IsEternal)
        {
            result.Append("Eternal ");
        }
        if (item.IsPerishable)
        {
            result.Append("Perishable ");
        }
        if (item.IsRental)
        {
            result.Append("Rental ");
        }

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
