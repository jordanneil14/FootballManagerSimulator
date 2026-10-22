namespace FootballManagerSimulator.Maui.Models;

public static class BoardConfidenceRules
{
    public const int Starting = 50;
    public const int Min = 0;
    public const int Max = 100;
    public const int Win = 3;
    public const int Draw = 0;
    public const int Loss = -3;

    public const int SecureFrom = 75;
    public const int StableFrom = 30;
    public const int UnderPressureFrom = 15;

    // AI clubs sack their manager below this and stay without one for a short spell.
    public const int SackedBelow = 10;
    public const int VacancyMinDays = 7;
    public const int VacancyMaxDays = 14;

    public static int Apply(int confidence, int change) => Math.Clamp(confidence + change, Min, Max);

    public static string Label(int confidence) => confidence switch
    {
        >= SecureFrom => "Secure",
        >= StableFrom => "Stable",
        >= UnderPressureFrom => "Under pressure",
        _ => "At risk"
    };
}
