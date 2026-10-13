namespace FootballManagerSimulator.Services;

/// <summary>Small presentation helpers shared across the Razor components.</summary>
public static class Ui
{
    public static string Money(int amount) => $"£{amount:n0}";

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
