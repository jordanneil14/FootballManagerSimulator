namespace FootballManagerSimulator.Maui.Models;

// English Cup prize money, paid to both clubs in every tie once the day's matches are over.
// Flat amounts, so a cup run means far more to a lower-division club than to a top-flight one.
public static class CupPrizeMoneyRules
{
    public static int ForLoser(int round) => round switch
    {
        <= 1 => 25_000,
        2 => 40_000,
        3 => 50_000,
        4 => 100_000,
        5 => 200_000,
        6 => 500_000,
        _ => 1_000_000
    };

    public static int ForWinner(int round) => ForLoser(round) * 2;

    // "round 3", "the quarter-final", ... for notifications. The last three rounds are named.
    public static string RoundName(int round, int finalRound) => (finalRound - round) switch
    {
        0 => "the final",
        1 => "the semi-final",
        2 => "the quarter-final",
        _ => $"round {round}"
    };
}
