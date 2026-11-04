using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Services;

/// <summary>Small presentation helpers shared across the Razor components.</summary>
public static class Ui
{
    public static string Money(int amount) => $"£{amount:n0}";

    private static readonly Dictionary<string, string> CountryFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["England"] = "🏴󠁧󠁢󠁥󠁮󠁧󠁿",
        ["Spain"] = "🇪🇸",
        ["Germany"] = "🇩🇪",
        ["Italy"] = "🇮🇹",
        ["France"] = "🇫🇷",
        ["Portugal"] = "🇵🇹",
        ["Scotland"] = "🏴󠁧󠁢󠁳󠁣󠁴󠁿",
        ["Belgium"] = "🇧🇪",
        ["Netherlands"] = "🇳🇱",
        ["Turkey"] = "🇹🇷",
        // Player nationalities.
        ["Wales"] = "🏴󠁧󠁢󠁷󠁬󠁳󠁿",
        ["Northern Ireland"] = "🇬🇧",
        ["Republic of Ireland"] = "🇮🇪",
        ["Brazil"] = "🇧🇷",
        ["Argentina"] = "🇦🇷",
        ["Colombia"] = "🇨🇴",
        ["Mexico"] = "🇲🇽",
        ["United States"] = "🇺🇸",
        ["Japan"] = "🇯🇵",
        ["Korea Republic"] = "🇰🇷",
        ["Denmark"] = "🇩🇰",
        ["Sweden"] = "🇸🇪",
        ["Norway"] = "🇳🇴",
        ["Poland"] = "🇵🇱",
        ["Russia"] = "🇷🇺",
        ["Croatia"] = "🇭🇷",
        ["Switzerland"] = "🇨🇭",
        ["Austria"] = "🇦🇹",
        ["Australia"] = "🇦🇺",
        ["Nigeria"] = "🇳🇬",
        ["Ghana"] = "🇬🇭",
        ["Ivory Coast"] = "🇨🇮",
        ["Senegal"] = "🇸🇳",
        ["Cameroon"] = "🇨🇲",
        ["Uruguay"] = "🇺🇾",
        ["Chile"] = "🇨🇱",
        ["Saudi Arabia"] = "🇸🇦",
        ["Czech Republic"] = "🇨🇿",
        ["Romania"] = "🇷🇴",
        ["Serbia"] = "🇷🇸",
        ["Greece"] = "🇬🇷",
        ["Ukraine"] = "🇺🇦",
        ["Morocco"] = "🇲🇦",
        ["Algeria"] = "🇩🇿",
        ["Tunisia"] = "🇹🇳",
        ["China PR"] = "🇨🇳",
        ["India"] = "🇮🇳",
        ["Paraguay"] = "🇵🇾",
        ["Ecuador"] = "🇪🇨",
        ["Peru"] = "🇵🇪",
        ["Venezuela"] = "🇻🇪",
        ["Bolivia"] = "🇧🇴",
        ["Costa Rica"] = "🇨🇷",
        ["Jamaica"] = "🇯🇲",
        ["Honduras"] = "🇭🇳",
        ["Finland"] = "🇫🇮",
        ["Iceland"] = "🇮🇸",
        ["Hungary"] = "🇭🇺",
    };

    public static string CountryFlag(string? countryName) =>
        countryName != null && CountryFlags.TryGetValue(countryName, out var flag) ? flag : "🏳️";

    private static readonly string[] PositionOrder =
    {
        "GK",
        "LB", "LWB", "LCB", "CB", "RCB", "RWB", "RB", "DEF",
        "CDM", "LM", "CM", "CAM", "RM", "MID",
        "LW", "LF", "LS", "CF", "ST", "RF", "RW", "ATT"
    };

    /// <summary>Sort key for squad lists: goalkeepers, defenders, midfielders, then attackers (uses the first listed position, e.g. "LW/ST" sorts as LW).</summary>
    public static int PositionSortOrder(string? position)
    {
        var first = (position ?? "").Split('/')[0].Trim().ToUpper();
        var index = Array.IndexOf(PositionOrder, first);
        return index < 0 ? PositionOrder.Length : index;
    }

    public static string PositionCssClass(string? position) => (position ?? "").ToUpper() switch
    {
        "GK" => "gk",
        "RB" or "LB" or "CB" or "RWB" or "LWB" or "DEF" => "def",
        "CDM" or "CM" or "CAM" or "RM" or "LM" or "MID" => "mid",
        "RW" or "LW" or "ST" or "CF" or "RF" or "LF" or "ATT" => "att",
        _ => ""
    };

    // Colours a starter's position and match-day rating by how well he knows the slot:
    // yellow (adjacent), amber (related), orange (unfamiliar), red (goalkeeper swap).
    public static string FamiliarityCssClass(PlayerModel? player, TacticSlotType slot) =>
        player == null ? "" : PositionFamiliarityRules.GetFamiliarity(player, slot) switch
        {
            PositionFamiliarity.Adjacent => "fam fam-adjacent",
            PositionFamiliarity.Related => "fam fam-related",
            PositionFamiliarity.Unfamiliar => "fam fam-unfamiliar",
            PositionFamiliarity.GoalkeeperSwap => "fam fam-gkswap",
            _ => ""
        };

    public static bool IsOutOfPosition(PlayerModel? player, TacticSlotType slot) =>
        player != null && PositionFamiliarityRules.GetFamiliarity(player, slot) != PositionFamiliarity.Natural;

    // Tooltip for an out-of-position starter, e.g. "Out of position (CB at RB): 80 → 68". Null when he's natural.
    public static string? FamiliarityTitle(PlayerModel? player, TacticSlotType slot)
    {
        if (player == null || PositionFamiliarityRules.GetFamiliarity(player, slot) == PositionFamiliarity.Natural)
            return null;
        return $"Out of position ({player.PreferredPosition} at {slot}): {player.Rating} → {PositionFamiliarityRules.EffectiveRating(player, slot)}";
    }

    public static string RatingCssClass(int rating) => rating switch
    {
        >= 82 => "rating-badge elite",
        >= 75 => "rating-badge good",
        _ => "rating-badge"
    };
}
