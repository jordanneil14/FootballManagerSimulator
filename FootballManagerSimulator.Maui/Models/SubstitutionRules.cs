namespace FootballManagerSimulator.Maui.Models;

// Substitutions in a competitive match, for the user and the AI alike: five, made in at most three
// stoppages. Changes at half-time, or in the break before extra time, don't use a stoppage, and extra
// time brings one more. Friendlies have no limits.
public static class SubstitutionRules
{
    public const int MaxSubstitutions = 5;
    public const int MaxStoppages = 3;
    public const int ExtraTimeStoppages = 1;

    public static bool IsBreak(int minute) => minute is 45 or 90;

    public static int StoppagesAllowed(int minute) => minute > 90 ? MaxStoppages + ExtraTimeStoppages : MaxStoppages;

    // The stoppages used by substitutions made at these minutes.
    public static int StoppagesUsed(IEnumerable<int> substitutionMinutes) =>
        substitutionMinutes.Where(m => !IsBreak(m)).Distinct().Count();

    // Whether a substitution can be made at this minute as far as stoppages go: it's a break, the side has
    // already stopped play to make a change this minute, or it has a stoppage left.
    public static bool CanStopPlayAt(IEnumerable<int> substitutionMinutes, int minute)
    {
        var minutes = substitutionMinutes.ToList();
        return IsBreak(minute) || minutes.Contains(minute) || StoppagesUsed(minutes) < StoppagesAllowed(minute);
    }
}
