using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Services;

/// <summary>
/// End-to-end tests over the real DI composition (GameServices.Register) and the real bundled
/// settings.json, driving the game the way the Razor pages do.
/// </summary>
public class FullGameTests : IDisposable
{
    private readonly GameHost _host = new();

    public void Dispose() => _host.Dispose();

    /// <summary>Plays the user's match the way the UI does, and lets the AI play the rest.</summary>
    private static void PlayToday(GameEngine engine)
    {
        if (engine.AdvanceTodaysFixtures() == GameEngine.TodaysFixturesResult.Results)
            return;

        engine.AutoPickTeam();
        var error = engine.StartMatch();
        if (error != null)
        {
            // e.g. too many suspensions to name a full side; let the AI simulate the user's game too.
            foreach (var fixture in engine.TodaysFixtures().Where(f => !f.Concluded).ToList())
                engine.MatchSimulator.ConcludeFixture(fixture, engine.GetCompetitionForFixture(fixture)!);
            return;
        }

        var mine = engine.MyFixtureToday()!;
        engine.MatchSimulator.ConcludeFixture(mine, engine.GetCompetitionForFixture(mine)!);
        engine.CompleteRemainingFixtures();
    }

    private static void AnswerBids(GameEngine engine)
    {
        foreach (var bid in engine.State.Notifications.Where(n => n.RequiresAction).ToList())
            engine.RejectBid(bid);
    }

    /// <summary>Advances day by day until <paramref name="until"/>, playing every fixture on the way.</summary>
    private static void PlayUntil(GameEngine engine, DateOnly until)
    {
        var guard = 0;
        while (engine.State.Date < until)
        {
            Assert.True(++guard < 2000, "Game stopped advancing");
            AnswerBids(engine);
            if (engine.Advance() == GameEngine.AdvanceResult.MustPlayFixtures)
                PlayToday(engine);
        }
    }

    [Fact]
    public void NewGame_BuildsTheWorldFromSettings()
    {
        var engine = _host.StartNewGame(clubId: 9, managerName: "jose mourinho");
        var state = engine.State;

        Assert.True(engine.GameInProgress);
        Assert.Equal("Jose Mourinho", state.ManagerName);
        Assert.Equal(new DateOnly(2026, 7, 1), state.Date);
        Assert.Equal(_host.Settings.Clubs.Count, state.Clubs.Count);
        Assert.Equal(_host.Settings.Competitions.Count(), state.Competitions.Count);
        Assert.Equal(state.Players.Count, state.Players.Select(p => p.Id).Distinct().Count());
        Assert.Contains(state.Players, p => p.Contract == null); // free agents exist

        foreach (var club in state.Clubs)
        {
            var squad = state.Players.Count(p => p.Contract?.ClubId == club.Id);
            Assert.InRange(squad, 25, 30);
            Assert.Equal(99, club.TacticSlots.Count);
        }

        foreach (var league in engine.Leagues)
        {
            var clubs = league.Clubs.Count;
            Assert.Equal(clubs * (clubs - 1), league.Fixtures.Count);
        }

        // Cup draws, the shield draw and the playoff events are all scheduled up front.
        Assert.Equal(7, state.Events.OfType<EnglishLeagueCupFixtureDrawEvent>().Count());
        Assert.Single(state.Events.OfType<EnglishCommunityShieldFixtureDrawEvent>());
        Assert.Equal(3, state.Events.OfType<EnglishFootballLeaguePlayoffEvent>().Count());
        Assert.Single(state.Events.OfType<NationalLeaguePlayoffQuarterFinalEvent>());

        Assert.Contains(state.Notifications, n => n.Subject.StartsWith("Welcome to "));
        Assert.NotEmpty(state.TransferListItems);
        Assert.Equal(3, state.Competitions.Single(c => c.Type == CompetitionType.Friendly).Fixtures.Count(f => f.HomeClub.Id == 9 || f.AwayClub.Id == 9));
    }

    [Fact]
    public void PlayingAWholeSeason_PromotesAndRelegatesAndStartsTheNextSeason()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var state = engine.State;
        var division1 = state.Clubs.Where(c => c.LeagueId == 1).Select(c => c.Id).ToHashSet();
        var division2 = state.Clubs.Where(c => c.LeagueId == 2).Select(c => c.Id).ToHashSet();
        LeagueModel? finalDivision1 = null, finalDivision2 = null;

        PlayUntil(engine, new DateOnly(2027, 6, 29));

        // Every competitive fixture of the season has been played.
        Assert.All(state.Competitions.SelectMany(c => c.Fixtures), f => Assert.True(f.Concluded));
        var cup = (CupModel)state.Competitions.Single(c => c.Type == CompetitionType.Cup);
        Assert.Single(cup.Fixtures, f => f.Round == 7);
        Assert.NotNull(cup.Fixtures.Single(f => f.Round == 7).ClubWon);
        Assert.Single(state.Competitions.Single(c => c.Id == 2).Fixtures, f => f.FixtureType == FixtureType.PlayoffFinal);
        finalDivision1 = (LeagueModel)state.Competitions.Single(c => c.Id == 1);
        finalDivision2 = (LeagueModel)state.Competitions.Single(c => c.Id == 2);
        var relegated = finalDivision1.GenerateLeagueTable().TakeLast(3).Select(p => p.Club.Id).ToList();
        var promoted = finalDivision2.GenerateLeagueTable().Take(2).Select(p => p.Club.Id).ToList();
        promoted.Add(finalDivision2.Fixtures.Single(f => f.FixtureType == FixtureType.PlayoffFinal).ClubWon!.Id);

        PlayUntil(engine, new DateOnly(2027, 6, 30)); // end-of-season processing runs on 30 June

        Assert.All(relegated, id => Assert.Equal(2, state.Clubs.Single(c => c.Id == id).LeagueId));
        Assert.All(promoted, id => Assert.Equal(1, state.Clubs.Single(c => c.Id == id).LeagueId));
        Assert.Equal(20, state.Clubs.Count(c => c.LeagueId == 1));
        Assert.Equal(24, state.Clubs.Count(c => c.LeagueId == 2));
        Assert.Contains(state.Notifications, n => n.Subject == "New Season");

        // The new season's competitions are built from the updated divisions.
        var newDivision1 = state.Competitions.OfType<LeagueModel>().Single(l => l.Id == 1);
        Assert.Equal(state.Clubs.Where(c => c.LeagueId == 1).Select(c => c.Id).OrderBy(i => i), newDivision1.Clubs.Select(c => c.Id).OrderBy(i => i));
        Assert.All(newDivision1.Fixtures, f => Assert.False(f.Concluded));
        Assert.All(state.Clubs, c => Assert.InRange(state.Players.Count(p => p.Contract?.ClubId == c.Id), 25, 40));
        Assert.DoesNotContain(state.Players, p => p.SuspensionMatches > 0);
        Assert.NotEqual(division1, state.Clubs.Where(c => c.LeagueId == 1).Select(c => c.Id).ToHashSet());
        Assert.NotEqual(division2, state.Clubs.Where(c => c.LeagueId == 2).Select(c => c.Id).ToHashSet());
    }

    [Fact]
    public void SaveAndLoad_RestoresTheGameIncludingScheduledEvents()
    {
        var engine = _host.StartNewGame(clubId: 9);
        PlayUntil(engine, new DateOnly(2026, 8, 20));
        var state = engine.State;
        var snapshot = new
        {
            state.Date,
            state.ManagerName,
            state.MyClubId,
            Players = state.Players.Count,
            Played = state.Competitions.SelectMany(c => c.Fixtures).Count(f => f.Concluded),
            PendingEvents = state.Events.Count(e => !e.IsCompleted),
            Budget = engine.MyClub.TransferBudget
        };

        Assert.Equal("Game saved successfully.", engine.SaveGame("midseason"));
        engine.StartNewGame(); // wipe the in-memory game
        Assert.Null(engine.LoadGame("midseason.fms"));

        Assert.Equal(snapshot, new
        {
            state.Date,
            state.ManagerName,
            state.MyClubId,
            Players = state.Players.Count,
            Played = state.Competitions.SelectMany(c => c.Fixtures).Count(f => f.Concluded),
            PendingEvents = state.Events.Count(e => !e.IsCompleted),
            Budget = engine.MyClub.TransferBudget
        });
        Assert.Contains(state.Competitions, c => c is LeagueModel); // polymorphic competitions survive

        // The loaded game carries on: the next cup draw still fires.
        PlayUntil(engine, new DateOnly(2026, 9, 5));
        Assert.Contains(state.Competitions.OfType<CupModel>().Single(c => c.Type == CompetitionType.Cup).Fixtures, f => f.Round == 2);
    }

    [Fact]
    public void SaveAndLoadBeforeSeasonEnd_StillPromotesAndRelegates()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var state = engine.State;
        PlayUntil(engine, new DateOnly(2027, 6, 29));

        Assert.Equal("Game saved successfully.", engine.SaveGame("endofseason"));
        engine.StartNewGame();
        Assert.Null(engine.LoadGame("endofseason.fms"));

        var division1 = (LeagueModel)state.Competitions.Single(c => c.Id == 1);
        var division2 = (LeagueModel)state.Competitions.Single(c => c.Id == 2);
        var relegated = division1.GenerateLeagueTable().TakeLast(3).Select(p => p.Club.Id).ToList();
        var promoted = division2.GenerateLeagueTable().Take(2).Select(p => p.Club.Id).ToList();
        promoted.Add(division2.Fixtures.Single(f => f.FixtureType == FixtureType.PlayoffFinal).ClubWon!.Id);

        PlayUntil(engine, new DateOnly(2027, 6, 30));

        Assert.All(relegated, id => Assert.Equal(2, state.Clubs.Single(c => c.Id == id).LeagueId));
        Assert.All(promoted, id => Assert.Equal(1, state.Clubs.Single(c => c.Id == id).LeagueId));
        var newDivision1 = state.Competitions.OfType<LeagueModel>().Single(l => l.Id == 1);
        Assert.All(promoted, id => Assert.Contains(newDivision1.Clubs, c => c.Id == id));
    }

    [Fact]
    public void RequestHigherTransferBudget_PaysOutAfterTwoDaysAndCannotBeSpammed()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var budget = engine.MyClub.TransferBudget;

        Assert.Equal("Transfer budget request has been submitted.", engine.RequestHigherTransferBudget());
        Assert.Contains("3 months", engine.RequestHigherTransferBudget());

        PlayUntil(engine, engine.State.Date.AddDays(2));

        // Bids for my players are all rejected, so the grant is the only change to my budget.
        Assert.Equal(budget + 10_000_000, engine.MyClub.TransferBudget);
        Assert.Contains(engine.State.Notifications, n => n.Subject == "Transfer Budget Request");
    }
}
