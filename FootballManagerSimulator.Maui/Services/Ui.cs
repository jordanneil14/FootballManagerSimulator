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

    public static string RatingCssClass(int rating) => rating switch
    {
        >= 82 => "rating-badge elite",
        >= 75 => "rating-badge good",
        _ => "rating-badge"
    };
}
