namespace FootballManagerSimulator.Maui.Models;

public static class TransferBudgetRequestRules
{
    // Roughly a fifth of a typical club's seasonal budget at each level, so a request matters
    // without dwarfing what the club already has.
    public static int AmountForLeagueRank(int? leagueRank) => leagueRank switch
    {
        1 => 10_000_000,
        2 => 2_500_000,
        3 => 1_000_000,
        4 => 150_000,
        _ => 50_000
    };
}
