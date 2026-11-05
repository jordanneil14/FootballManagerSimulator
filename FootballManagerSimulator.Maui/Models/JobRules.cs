namespace FootballManagerSimulator.Maui.Models;

// How keen a club is on you, from your reputation against its standing. Only the label is ever shown.
public enum JobChance { Interested, Possible, LongShot, OutOfReach }

public static class JobRules
{
    public const double Min = 0;
    public const double Max = 100;

    // A new career starts just below the club you chose, so better jobs are possible but not straight away.
    public const double StartingBelowClub = 10;
    // The lowest a starting reputation, or a spell out of work, can take you.
    public const double Floor = 5;
    // Old saves with no clubs in the manager history start here.
    public const double NoHistoryStart = 20;

    // Bands by gap (reputation minus club standing, after adjustments).
    public const double InterestedFrom = -5;
    public const double PossibleFrom = -15;
    public const double LongShotFrom = -25;

    public const double InterestedChance = 0.9;
    public const double PossibleChance = 0.5;
    public const double LongShotChance = 0.15;
    // Clubs far below you wonder whether you'd stay.
    public const double WellBelowGap = 30;
    public const double WellBelowChance = 0.6;

    public const double UnderPressurePenalty = 5;
    public const double SecureBonus = 3;

    public const int MaxPendingApplications = 3;
    public const int DecisionMinDays = 2;
    public const int DecisionMaxDays = 4;
    public const int RejectionCooldownDays = 90;

    // Unsolicited offers come from clubs at your level or a step above.
    public const double OfferMinGap = -10;
    public const double OfferMaxGap = 5;

    // Out of work: a reachable job is opened up after this long, and clubs stop being fussy after this.
    public const int DeadEndDays = 28;
    public const int DesperateDays = 90;
    // ...and reputation slips by a point every 30 days after the first 90.
    public const int DeclineAfterDays = 90;
    public const int DeclineEveryDays = 30;
    public const double DeclinePerStep = 1;

    // End of season.
    public const double FinishPerPlace = 1.5;
    public const double FinishMaxChange = 10;
    public const double DriftShare = 0.10;
    public const double LeagueTitle = 8;
    public const double CupWin = 5;
    public const double Promotion = 5;
    public const double Relegation = -6;

    public const double Sacked = -8;
    public const double Resigned = -3;

    public const double WeightMin = 0.5;
    public const double WeightMax = 1.5;

    public static double Apply(double reputation, double change) => Math.Clamp(reputation + change, Min, Max);

    public static JobChance ChanceFor(double gap) => gap switch
    {
        >= InterestedFrom => JobChance.Interested,
        >= PossibleFrom => JobChance.Possible,
        >= LongShotFrom => JobChance.LongShot,
        _ => JobChance.OutOfReach
    };

    public static double AcceptChance(double gap, int daysUnemployed)
    {
        var band = ChanceFor(gap);
        if (daysUnemployed >= DesperateDays && band is JobChance.Interested or JobChance.Possible)
            return InterestedChance;

        return band switch
        {
            JobChance.Interested => gap > WellBelowGap ? WellBelowChance : InterestedChance,
            JobChance.Possible => PossibleChance,
            JobChance.LongShot => LongShotChance,
            _ => 0
        };
    }

    // The share of the scale goes into the league weight: an average league is worth 1.
    public static double Weight(double averageStanding) => Math.Clamp(averageStanding / 50, WeightMin, WeightMax);

    public static double FinishChange(int expected, int actual, double weight, double share) =>
        Math.Clamp((expected - actual) * FinishPerPlace, -FinishMaxChange, FinishMaxChange) * weight * share;

    // 0–20 ★, 20–40 ★★ ... 80–100 ★★★★★.
    public static int Stars(double value) => Math.Clamp((int)Math.Ceiling(value / 20), 1, 5);

    public static string StarsText(double value) => new('★', Stars(value));

    public static string Format(double value) => $"{value:0}";

    public static string Label(JobChance chance) => chance switch
    {
        JobChance.Interested => "Interested",
        JobChance.Possible => "Possible",
        JobChance.LongShot => "Long shot",
        _ => "Out of reach"
    };

    public static string Colour(JobChance chance) => chance switch
    {
        JobChance.Interested => "var(--accent)",
        JobChance.Possible => "var(--warn)",
        JobChance.LongShot => "var(--danger)",
        _ => "var(--text-faint)"
    };

    // Seasons run July to June.
    public static DateOnly SeasonEnd(DateOnly date) => new(date.Month >= 7 ? date.Year + 1 : date.Year, 6, 30);
}
