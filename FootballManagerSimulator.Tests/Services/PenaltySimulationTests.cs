using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;
using FootballManagerSimulator.Tests.Support;
using Xunit.Abstractions;

namespace FootballManagerSimulator.Tests.Services;

// Checks penalties against the real game data. Run with RUN_SIMULATIONS=1.
[Collection(SaveFilesCollection.Name)]
public class PenaltySimulationTests(ITestOutputHelper output) : IDisposable
{
    private readonly GameHost _host = new();

    public void Dispose() => _host.Dispose();

    // Every league pairing at the start of a new game, weighted by how often each side wins a penalty.
    // The reference values are set from the averages this prints, so the average penalty is scored 78% of the time.
    [SimulationFact]
    public void Calibration_AveragePenaltyIsScoredAtTheTargetRate()
    {
        var engine = _host.Engine;
        engine.StartNewGame();
        var state = _host.State;
        var clubs = _host.Get<IClubHelper>();
        var players = _host.Get<IPlayerHelper>();

        var sides = new Dictionary<int, (double Rating, PlayerModel Taker, PlayerModel? Keeper)>();
        foreach (var club in state.Clubs)
        {
            engine.MatchSimulator.PrepareClub(club.Id);
            var onPitch = clubs.GetStartingElevenByClub(club.Id)
                .Where(s => s.PlayerId != null)
                .Select(s => (Slot: s, Player: players.GetPlayerById(s.PlayerId!.Value)!))
                .ToList();
            var taker = MatchSimulatorHelper.PickPenaltyTaker(club, onPitch);
            if (taker == null) continue;
            var keeper = onPitch.FirstOrDefault(p => p.Slot.TacticSlotType == TacticSlotType.GK).Player;
            sides[club.Id] = (Math.Pow(clubs.GetStartingElevenSumRatingForClub(club.Id), MatchSimulatorHelper.RatingGapExponent), taker, keeper);
        }

        double weight = 0, conversion = 0, takerScore = 0, keeperScore = 0, keeperWeight = 0;
        foreach (var league in state.Clubs.GroupBy(c => c.LeagueId))
        {
            var ids = league.Select(c => c.Id).Where(sides.ContainsKey).ToList();
            foreach (var home in ids)
            foreach (var away in ids.Where(a => a != home))
            {
                var (homeRating, homeTaker, homeKeeper) = sides[home];
                var (awayRating, awayTaker, awayKeeper) = sides[away];
                var homeShare = homeRating / (homeRating + awayRating * 0.8);

                foreach (var (share, taker, keeper) in new[] { (homeShare, homeTaker, awayKeeper), (1 - homeShare, awayTaker, homeKeeper) })
                {
                    weight += share;
                    conversion += share * MatchSimulatorHelper.ScoringChance(taker, keeper);
                    takerScore += share * MatchSimulatorHelper.TakerScore(taker);
                    if (keeper == null) continue;
                    keeperWeight += share;
                    keeperScore += share * MatchSimulatorHelper.KeeperScore(keeper);
                }
            }
        }

        output.WriteLine($"Clubs: {sides.Count}");
        output.WriteLine($"Average taker score: {takerScore / weight:F2} (reference {MatchSimulatorHelper.TakerReference})");
        output.WriteLine($"Average keeper score: {keeperScore / keeperWeight:F2} (reference {MatchSimulatorHelper.KeeperReference})");
        output.WriteLine($"Expected conversion: {conversion / weight:P2}");

        Assert.InRange(conversion / weight, 0.77, 0.79);
    }

    // A whole season of real matches: goals per match, how often penalties happen and how many go in.
    [SimulationFact]
    public void OneSeason_PenaltiesHitTheirTargetsAndGoalsPerMatchHold()
    {
        var engine = _host.StartNewGame();
        var state = _host.State;
        var seasonEnd = new DateOnly(state.Date.Year + 1, 5, 31);

        var guard = 0;
        while (state.Date < seasonEnd)
        {
            if (++guard > 1000) throw new Exception($"Game stuck at {state.Date}");
            foreach (var pending in state.Notifications.Where(n => n.RequiresAction).ToList())
                pending.RequiresAction = false;

            if (engine.Advance() != GameEngine.AdvanceResult.MustPlayFixtures)
                continue;

            if (engine.AdvanceTodaysFixtures() == GameEngine.TodaysFixturesResult.Results)
                continue;

            engine.AutoPickTeam();
            foreach (var fixture in engine.TodaysFixtures().Where(f => !f.Concluded).ToList())
                engine.MatchSimulator.ConcludeFixture(fixture, engine.GetCompetitionForFixture(fixture)!);
            engine.CompleteRemainingFixtures();
        }

        var matches = state.Competitions
            .Where(c => c.Type != CompetitionType.Friendly)
            .SelectMany(c => c.Fixtures)
            .Where(f => f.Concluded)
            .ToList();
        var penalties = matches.SelectMany(f => f.HomePenalties.Concat(f.AwayPenalties)).ToList();

        var goalsPerMatch = matches.Average(f => f.HomeScorers.Concat(f.AwayScorers).Count(g => g.Minute <= 90));
        var withPenalty = matches.Count(f => f.HomePenalties.Count + f.AwayPenalties.Count > 0) / (double)matches.Count;
        var scored = penalties.Count(p => p.Outcome == PenaltyOutcome.Scored) / (double)penalties.Count;
        var saved = penalties.Count(p => p.Outcome == PenaltyOutcome.Saved) / (double)penalties.Count;

        output.WriteLine($"Matches: {matches.Count}, penalties: {penalties.Count}");
        output.WriteLine($"Goals per match in 90 minutes: {goalsPerMatch:F3} (before penalties: {MatchSimulatorHelper.BaseGoalsPerMatch})");
        output.WriteLine($"Matches with a penalty: {withPenalty:P1}");
        output.WriteLine($"Penalties scored: {scored:P1}, saved: {saved:P1}, missed: {1 - scored - saved:P1}");

        // A season is a few thousand matches, so allow for chance.
        Assert.InRange(goalsPerMatch, MatchSimulatorHelper.BaseGoalsPerMatch - 0.08, MatchSimulatorHelper.BaseGoalsPerMatch + 0.08);
        Assert.InRange(withPenalty, 0.20, 0.25);
        Assert.InRange(scored, 0.74, 0.82);
    }
}
