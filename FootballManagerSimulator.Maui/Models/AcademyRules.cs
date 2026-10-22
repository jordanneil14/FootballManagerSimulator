using FootballManagerSimulator.Maui.Helpers;

namespace FootballManagerSimulator.Maui.Models;

public static class AcademyRules
{
    public const int Min = 1;
    public const int Max = 10;

    // No club starts above this; anything better has to be built.
    public const int StartingMax = 6;

    public const int UpgradeMonths = 3;

    // Clubs in higher leagues start with better academies.
    public static int StartingRating(int leagueRank) => leagueRank switch
    {
        1 => RandomNumberHelper.Next(4, StartingMax + 1),
        2 => RandomNumberHelper.Next(3, 6),
        3 => RandomNumberHelper.Next(2, 5),
        4 => RandomNumberHelper.Next(1, 4),
        _ => RandomNumberHelper.Next(1, 3)
    };

    // What it costs to take the academy up to the given rating.
    public static int UpgradeCost(int targetRating) => targetRating switch
    {
        <= 2 => 250_000,
        3 => 500_000,
        4 => 1_000_000,
        5 => 2_000_000,
        6 => 3_500_000,
        7 => 5_000_000,
        8 => 8_000_000,
        9 => 12_000_000,
        _ => 18_000_000
    };
}
