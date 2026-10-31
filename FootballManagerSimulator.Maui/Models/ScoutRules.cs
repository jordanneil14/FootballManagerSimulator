namespace FootballManagerSimulator.Maui.Models;

public static class ScoutRules
{
    public const int MonthlyWage = 3_000;
    public const int MaxScouts = 5;
    public const int MaxPlayersPerReport = 3;
    public const int ReportIntervalDays = 14;

    // The players the club already knows about when a new career starts.
    public const int StartingDiscoveredMin = 100;
    public const int StartingDiscoveredMax = 150;
    public const double StartingFreeAgentShare = 0.4;
}
