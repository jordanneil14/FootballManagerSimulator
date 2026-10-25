namespace FootballManagerSimulator.Maui.Models;

public static class BoardConfidenceRules
{
    public const int Starting = 50;
    public const int Min = 0;
    public const int Max = 100;
    public const int Win = 3;
    public const int Draw = 0;
    public const int Loss = -3;
    public const int Promotion = 10;

    public const int SecureFrom = 75;
    public const int StableFrom = 30;
    public const int UnderPressureFrom = 15;

    // AI clubs sack their manager below this and stay without one for a short spell.
    public const int SackedBelow = 10;

    // The board sacks you when its confidence falls below this.
    public const int UserSackedBelow = 5;
    public const int VacancyMinDays = 7;
    public const int VacancyMaxDays = 14;

    // A new game opens with a few clubs already looking for a manager.
    public const int StartingVacanciesMin = 2;
    public const int StartingVacanciesMax = 4;

    // AI clubs open a new game with varied job security, and a few managers already under threat.
    public const int StartingAiSecurityMin = 25;
    public const int StartingAiSecurityMax = 85;
    public const int StartingUnderThreatMin = 2;
    public const int StartingUnderThreatMax = 4;

    public static int Apply(int confidence, int change) => Math.Clamp(confidence + change, Min, Max);

    public static string Label(int confidence) => confidence switch
    {
        >= SecureFrom => "Secure",
        >= StableFrom => "Stable",
        >= UnderPressureFrom => "Under pressure",
        _ => "At risk"
    };
}
