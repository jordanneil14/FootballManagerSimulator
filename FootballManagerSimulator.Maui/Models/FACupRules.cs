namespace FootballManagerSimulator.Maui.Models;

// The English FA Cup: 80 clubs in round 1 (Divisions 3 and 4 plus non-league clubs drawn at random),
// Divisions 1 and 2 joining in round 3 to make 64, then straight knockout to a May final.
public static class FACupRules
{
    public const int Rounds = 8;
    public const int RoundOneClubs = 80;
    public const int TopFlightEntryRound = 3;
    public const int RoundOneDrawDaysBefore = 14;

    // League ranks within England that enter in round 1, by right or by random draw, and in round 3.
    public static readonly int[] RoundOneRanks = [3, 4];
    public static readonly int[] NonLeagueRanks = [5, 6];
    public static readonly int[] RoundThreeRanks = [1, 2];

    public static readonly TimeOnly KickOff = new(15, 0);

    // Saturdays, for a season starting in July of seasonStartYear.
    public static DateOnly PlannedDate(int round, int seasonStartYear) => round switch
    {
        1 => NthSaturday(seasonStartYear, 11, 1),
        2 => NthSaturday(seasonStartYear, 12, 1),
        3 => NthSaturday(seasonStartYear + 1, 1, 1),
        4 => LastSaturday(seasonStartYear + 1, 1),
        5 => LastSaturday(seasonStartYear + 1, 2),
        6 => NthSaturday(seasonStartYear + 1, 3, 3),
        7 => NthSaturday(seasonStartYear + 1, 4, 2),
        8 => NthSaturday(seasonStartYear + 1, 5, 3),
        _ => throw new ArgumentOutOfRangeException(nameof(round))
    };

    // Both clubs in a tie are paid, the winner double.
    public static int ForLoser(int round) => round switch
    {
        <= 1 => 25_000,
        2 => 45_000,
        3 => 65_000,
        4 => 95_000,
        5 => 150_000,
        6 => 300_000,
        7 => 600_000,
        _ => 1_250_000
    };

    public static int ForWinner(int round) => ForLoser(round) * 2;

    private static DateOnly NthSaturday(int year, int month, int n)
    {
        var date = new DateOnly(year, month, 1);
        while (date.DayOfWeek != DayOfWeek.Saturday)
            date = date.AddDays(1);
        return date.AddDays(7 * (n - 1));
    }

    private static DateOnly LastSaturday(int year, int month)
    {
        var date = new DateOnly(year, month, 1).AddMonths(1).AddDays(-1);
        while (date.DayOfWeek != DayOfWeek.Saturday)
            date = date.AddDays(-1);
        return date;
    }
}
