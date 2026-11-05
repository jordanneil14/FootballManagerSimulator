using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;

namespace FootballManagerSimulator.Maui.Helpers;

// What the season did for your reputation, worked out before promotion and relegation move the clubs.
public record SeasonReview(int ClubId, int CountryId, int LeagueRank, double Change, List<string> Reasons);

/// <summary>
/// Club standing (where a squad ranks among every club) and the manager's reputation, which together
/// decide which clubs will have you. See docs/specs/job-eligibility.md.
/// </summary>
public class ReputationHelper(IState state, INotificationFactory notificationFactory) : IReputationHelper
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    // Old saves and new careers start just below their club.
    public double Reputation => State.ManagerReputation ??= StartingReputation();

    public void Change(double amount) => State.ManagerReputation = JobRules.Apply(Reputation, amount);

    private double StartingReputation()
    {
        var clubId = State.MyClubId ?? State.ManagerHistory.Jobs.LastOrDefault()?.ClubId;
        if (clubId == null || !State.Clubs.Any(p => p.Id == clubId))
            return JobRules.NoHistoryStart;

        return Math.Max(JobRules.Floor, Standings().GetValueOrDefault(clubId.Value) - JobRules.StartingBelowClub);
    }

    // The average rating of each club's best players: the same measure as the user's squad average.
    public static Dictionary<int, double> SquadStrengths(IEnumerable<PlayerModel> players) => players
        .Where(p => p.Contract != null)
        .GroupBy(p => p.Contract!.ClubId)
        .ToDictionary(
            g => g.Key,
            g => g.Select(p => (double)p.Rating).OrderByDescending(r => r).Take(GameEngine.SquadAverageBestPlayers).Average());

    // Each club's percentile by squad strength, 0 (weakest) to 100 (strongest). Equal squads share a value.
    public static Dictionary<int, double> Standings(IEnumerable<ClubModel> clubs, IEnumerable<PlayerModel> players)
    {
        var strengths = SquadStrengths(players);
        var clubStrengths = clubs.Select(c => (c.Id, Strength: strengths.GetValueOrDefault(c.Id))).ToList();
        if (clubStrengths.Count == 1)
            return new() { [clubStrengths[0].Id] = 50 };

        var sorted = clubStrengths.Select(p => p.Strength).OrderBy(s => s).ToList();
        var result = new Dictionary<int, double>();
        foreach (var (id, strength) in clubStrengths)
        {
            var below = LowerBound(sorted, strength);
            var equal = LowerBound(sorted, Math.BitIncrement(strength)) - below;
            result[id] = 100.0 * (below + (equal - 1) / 2.0) / (sorted.Count - 1);
        }
        return result;
    }

    private static int LowerBound(List<double> sorted, double value)
    {
        int lo = 0, hi = sorted.Count;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (sorted[mid] < value) lo = mid + 1; else hi = mid;
        }
        return lo;
    }

    public IReadOnlyDictionary<int, double> Standings() => Standings(State.Clubs, State.Players);

    public double LeagueWeight(int leagueId, IReadOnlyDictionary<int, double> standings) =>
        Weight(State.Clubs.Where(p => p.LeagueId == leagueId).Select(p => p.Id), standings);

    private static double Weight(IEnumerable<int> clubIds, IReadOnlyDictionary<int, double> standings)
    {
        var values = clubIds.Select(id => standings.GetValueOrDefault(id)).ToList();
        return values.Count == 0 ? 1 : JobRules.Weight(values.Average());
    }

    // Where the club's squad ranks among the clubs in its league.
    public int? ExpectedFinish(ClubModel club)
    {
        var strengths = SquadStrengths(State.Players);
        var ranked = State.Clubs
            .Where(p => p.LeagueId == club.LeagueId)
            .OrderByDescending(p => strengths.GetValueOrDefault(p.Id))
            .ThenBy(p => p.Id)
            .Select(p => p.Id)
            .ToList();
        var index = ranked.IndexOf(club.Id);
        return index < 0 ? null : index + 1;
    }

    public void SetExpectedFinish()
    {
        var club = State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId);
        if (club != null && State.ManagerHistory.CurrentJob is { } job)
            job.ExpectedFinish = ExpectedFinish(club);
    }

    public SeasonReview? ReviewSeason()
    {
        var club = State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId);
        var league = State.Competitions.OfType<LeagueModel>().FirstOrDefault(p => p.Id == club?.LeagueId);
        var job = State.ManagerHistory.CurrentJob;
        if (club == null || league == null || job == null)
            return null;

        var standings = Standings();
        var weight = LeagueWeight(league.Id, standings);
        var change = 0.0;
        var reasons = new List<string>();

        var table = league.GenerateLeagueTable().Select(p => p.Club.Id).ToList();
        var actual = table.IndexOf(club.Id) + 1;
        var expected = job.ExpectedFinish ?? ExpectedFinish(club) ?? actual;
        if (actual > 0)
        {
            change += JobRules.FinishChange(expected, actual, weight, LeagueShareManaged(league, club, job));
            var places = expected - actual;
            var versus = places switch
            {
                > 0 => $"{places} place{(places == 1 ? "" : "s")} above expectations",
                < 0 => $"{-places} place{(places == -1 ? "" : "s")} below expectations",
                _ => "as expected"
            };
            reasons.Add($"Finished {NumberHelper.AddOrdinal(actual)} in {league.Name}, {versus}.");

            if (actual == 1)
            {
                change += JobRules.LeagueTitle * weight;
                reasons.Add($"Won {league.Name}.");
            }
        }

        foreach (var cup in CupsWonThisSeason())
        {
            var competition = State.Competitions.FirstOrDefault(p => p.Type.IsKnockout() && p.Name == cup);
            var cupWeight = competition == null ? 1 : Weight(
                competition.Fixtures.SelectMany(f => new[] { f.HomeClub.Id, f.AwayClub.Id }).Distinct(), standings);
            change += JobRules.CupWin * cupWeight;
            reasons.Add($"Won {cup}.");
        }

        var drift = (standings.GetValueOrDefault(club.Id) - Reputation) * JobRules.DriftShare;
        if (drift > 0)
            change += drift;

        return new SeasonReview(club.Id, league.CountryId, league.Rank, change, reasons);
    }

    // Trophies other than league titles and play-off finals (promotion counts for those).
    private IEnumerable<string> CupsWonThisSeason()
    {
        var season = ManagerHistoryModel.SeasonLabel(State.Date);
        var leagueNames = State.Competitions.OfType<LeagueModel>().Select(p => p.Name).ToHashSet();
        return State.ManagerHistory.Trophies
            .Where(t => t.Season == season && !leagueNames.Contains(t.Name) && !t.Name.EndsWith("Play-off Final"))
            .Select(t => t.Name);
    }

    // A manager who arrived partway through only answers for the games they were in charge of.
    private static double LeagueShareManaged(LeagueModel league, ClubModel club, ManagerJobModel job)
    {
        var games = league.Fixtures
            .Where(f => f.FixtureType == FixtureType.Base && f.Concluded && (f.HomeClub.Id == club.Id || f.AwayClub.Id == club.Id))
            .ToList();
        return games.Count == 0 ? 1 : (double)games.Count(f => f.Date >= job.StartDate) / games.Count;
    }

    public void CompleteSeason(SeasonReview? review)
    {
        if (review == null || State.MyClubId != review.ClubId)
        {
            State.LastSeasonReputationChange = null;
            return;
        }

        var change = review.Change;
        var club = State.Clubs.First(p => p.Id == review.ClubId);
        var league = State.Competitions.OfType<LeagueModel>().FirstOrDefault(p => p.Id == club.LeagueId);
        if (league != null && league.CountryId == review.CountryId && league.Rank != review.LeagueRank)
        {
            var promoted = league.Rank < review.LeagueRank;
            change += promoted ? JobRules.Promotion : JobRules.Relegation;
            review.Reasons.Add(promoted ? "Promoted." : "Relegated.");
        }

        var before = Reputation;
        Change(change);
        var after = Reputation;
        State.LastSeasonReputationChange = after - before;

        var direction = Math.Round(after) > Math.Round(before) ? "risen to" : Math.Round(after) < Math.Round(before) ? "fallen to" : "stayed at";
        NotificationFactory.AddNotification(
            State.Date,
            "Assistant Manager",
            "Your Reputation",
            $"Your reputation has {direction} {JobRules.Format(after)} ({JobRules.StarsText(after)}).\n\t{string.Join("\n\t", review.Reasons)}");
    }
}
