namespace FootballManagerSimulator.Maui.Models;

// A club's budget for a season: its starting budget in a new game, and what the board adds every
// end of season. It follows the league the club is in now, so it rises with promotion, falls with
// relegation, and grows as the stadium is expanded.
//   budget = division base x country factor x club size factor
public static class SeasonalBudgetRules
{
    // A typical club's budget at each level of the pyramid, in a country whose top flight
    // averages ReferenceStadiumSize seats.
    public static int DivisionBase(int leagueRank) => leagueRank switch
    {
        1 => 50_000_000,
        2 => 12_000_000,
        3 => 4_500_000,
        4 => 1_000_000,
        5 => 250_000,
        _ => 100_000
    };

    public const int ReferenceStadiumSize = 38_000;

    // Bigger football countries (judged by their top flight's crowds) have richer leagues all the way down.
    public const double MinCountryFactor = 0.1;
    public const double MaxCountryFactor = 2.0;

    // How far a club's stadium, compared with its league's average, can lift or cut its budget.
    public const double MinClubFactor = 0.5;
    public const double MaxClubFactor = 2.0;

    // Budgets for every club in a known league, keyed by club id.
    public static Dictionary<int, int> Budgets(IEnumerable<ClubModel> clubs, IEnumerable<CompetitionModel> competitions)
    {
        var leagues = competitions.Where(p => p.IsLeague).ToDictionary(p => p.Id);
        var known = clubs.Where(p => leagues.ContainsKey(p.LeagueId)).ToList();

        var leagueStadiums = known
            .GroupBy(p => p.LeagueId)
            .ToDictionary(g => g.Key, g => g.Average(p => (double)p.StadiumSize));

        var topFlightStadiums = known
            .Where(p => leagues[p.LeagueId].Rank == 1)
            .GroupBy(p => leagues[p.LeagueId].CountryId)
            .ToDictionary(g => g.Key, g => g.Average(p => (double)p.StadiumSize));

        return known.ToDictionary(p => p.Id, club =>
        {
            var league = leagues[club.LeagueId];

            var countryFactor = topFlightStadiums.TryGetValue(league.CountryId, out var topFlight)
                ? Math.Clamp(Math.Pow(topFlight / ReferenceStadiumSize, 2), MinCountryFactor, MaxCountryFactor)
                : 1.0;

            var leagueStadium = leagueStadiums[club.LeagueId];
            var clubFactor = leagueStadium > 0
                ? Math.Clamp(club.StadiumSize / leagueStadium, MinClubFactor, MaxClubFactor)
                : 1.0;

            return RoundToThreeFigures(DivisionBase(league.Rank) * countryFactor * clubFactor);
        });
    }

    // AI clubs can't save beyond this many seasons' allocation, so their spending power stays in line
    // with the league they're in. Relegated clubs drop to their new league's cap straight away.
    public const double AIBudgetCapSeasons = 1.5;

    // The user's club can't carry anything over: unspent money goes back to the board each season,
    // so it has to be spent (on players, the stadium or the academy) to be worth anything.
    public const double MyClubBudgetCapSeasons = 1.0;

    // The most a club can hold after the end-of-season allocation.
    public static int AIBudgetCap(int allocation) => Cap(allocation, AIBudgetCapSeasons);
    public static int MyClubBudgetCap(int allocation) => Cap(allocation, MyClubBudgetCapSeasons);

    private static int Cap(int allocation, double seasons) => (int)Math.Min(allocation * seasons, int.MaxValue);

    // A club's budget after the end-of-season allocation (already for its new league), capped at
    // AIBudgetCapSeasons x the allocation for AI clubs and MyClubBudgetCapSeasons x for the user's.
    public static int NextBudget(int current, int allocation, bool isAIClub) =>
        (int)Math.Min((long)current + allocation, isAIClub ? AIBudgetCap(allocation) : MyClubBudgetCap(allocation));

    // £52,345,678 → £52,300,000, so budgets read like a board would set them.
    private static int RoundToThreeFigures(double amount)
    {
        if (amount < 1_000) return (int)Math.Round(amount);
        var step = Math.Pow(10, Math.Floor(Math.Log10(amount)) - 2);
        return (int)Math.Min(Math.Round(amount / step) * step, int.MaxValue);
    }
}
