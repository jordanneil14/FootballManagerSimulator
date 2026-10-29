using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Services;

/// <summary>
/// End-to-end tests over the real DI composition (GameServices.Register) and the real bundled
/// settings.json, driving the game the way the Razor pages do.
/// </summary>
[Collection(SaveFilesCollection.Name)]
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
        foreach (var pending in engine.State.Notifications.Where(n => n.RequiresAction).ToList())
        {
            if (pending.JobOfferClubId != null || pending.IsBoardWarning)
                pending.RequiresAction = false; // decline offers to stay at the club; acknowledge warnings
            else
                engine.RejectBid(pending);
        }
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
        Assert.All(state.Clubs, c => Assert.InRange(c.AcademyRating, AcademyRules.Min, AcademyRules.StartingMax));

        // Starting budgets come from the seasonal budget rule, and every English division averages more than the one below.
        Assert.Equal(SeasonalBudgetRules.Budgets(state.Clubs, _host.Settings.Competitions), state.Clubs.ToDictionary(c => c.Id, c => c.TransferBudget));
        var englishLeagues = _host.Settings.Competitions.Where(c => c.IsLeague && c.CountryId == 1).ToList();
        var averageByRank = englishLeagues
            .GroupBy(l => l.Rank)
            .OrderBy(g => g.Key)
            .Select(g => state.Clubs.Where(c => g.Any(l => l.Id == c.LeagueId)).Average(c => c.TransferBudget))
            .ToList();
        Assert.Equal(averageByRank.OrderByDescending(a => a), averageByRank);

        foreach (var club in state.Clubs)
        {
            var squad = state.Players.Count(p => p.Contract?.ClubId == club.Id);
            Assert.InRange(squad, 25, 30);
            Assert.Equal(99, club.TacticSlots.Count);
            Assert.Contains(club.Formation, TacticHelper.Formations);
        }

        // Clubs start with a random formation rather than all defaulting to 4-4-2.
        Assert.True(state.Clubs.Select(c => c.Formation).Distinct().Count() > 1);

        foreach (var league in engine.Leagues)
        {
            // The split league meets three times before splitting; its last round is drawn later.
            var clubs = league.Clubs.Count;
            var meetings = league.Type == CompetitionType.ScottishPremiership ? 3 : 2;
            Assert.Equal(clubs * (clubs - 1) / 2 * meetings, league.Fixtures.Count);
        }

        // Cup draws, the shield draw and the playoff events are all scheduled up front.
        Assert.Equal(7, state.Events.OfType<EnglishLeagueCupFixtureDrawEvent>().Count());
        Assert.Single(state.Events.OfType<EnglishCommunityShieldFixtureDrawEvent>());
        Assert.Equal(["Manchester Blue", "North London Reds"],
            state.Competitions.Single(c => c.Type == CompetitionType.EnglishCommunityShield).Clubs.Select(c => c.Name));
        Assert.Equal(3, state.Events.OfType<EnglishFootballLeaguePlayoffEvent>().Count());
        Assert.Equal(3, state.Events.OfType<NationalLeaguePlayoffQuarterFinalEvent>().Count());
        Assert.Single(state.Events.OfType<ScottishPremiershipSplitEvent>());

        Assert.Contains(state.Notifications, n => n.Subject.StartsWith("Welcome to "));
        Assert.NotEmpty(state.TransferListItems);
        Assert.Equal(3, state.Competitions.Single(c => c.Type == CompetitionType.Friendly).Fixtures.Count(f => f.HomeClub.Id == 9 || f.AwayClub.Id == 9));
    }

    [Fact]
    public void NewGame_OpensWithAFewVacantJobsElsewhere()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var vacant = engine.State.Clubs.Where(c => c.VacantUntil != null).ToList();

        Assert.InRange(vacant.Count, BoardConfidenceRules.StartingVacanciesMin, BoardConfidenceRules.StartingVacanciesMax);
        Assert.DoesNotContain(vacant, c => c.Id == 9);
        Assert.All(vacant, c => Assert.InRange(c.VacantUntil!.Value.DayNumber - engine.State.Date.DayNumber,
            BoardConfidenceRules.VacancyMinDays, BoardConfidenceRules.VacancyMaxDays));
    }

    [Fact]
    public void NewGame_OpensWithVariedJobSecurityAndAFewManagersUnderThreat()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var aiClubs = engine.State.Clubs.Where(c => c.Id != 9 && c.VacantUntil == null).ToList();
        var underThreat = aiClubs.Where(c => c.JobSecurity < BoardConfidenceRules.UnderPressureFrom).ToList();

        Assert.InRange(underThreat.Count, BoardConfidenceRules.StartingUnderThreatMin, BoardConfidenceRules.StartingUnderThreatMax);
        Assert.All(underThreat, c => Assert.True(c.JobSecurity >= BoardConfidenceRules.SackedBelow));
        Assert.All(aiClubs.Except(underThreat), c => Assert.InRange(c.JobSecurity,
            BoardConfidenceRules.StartingAiSecurityMin, BoardConfidenceRules.StartingAiSecurityMax));
        Assert.True(aiClubs.Select(c => c.JobSecurity).Distinct().Count() > 5);
    }

    [Fact]
    public void LosingTheBoardsConfidence_GetsMeSackedAndTheGameCarriesOnUntilIFindAJob()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var state = engine.State;
        PlayUntil(engine, new DateOnly(2026, 8, 20));

        state.BoardConfidence = BoardConfidenceRules.UserSackedBelow - 1;
        PlayUntil(engine, state.Date.AddDays(1));

        Assert.True(engine.IsUnemployed);
        Assert.True(engine.GameInProgress);
        Assert.Null(engine.CurrentClub);
        Assert.Null(engine.MyCompetition);
        Assert.Contains(state.Notifications, n => n.Subject.StartsWith("Sacked by"));

        // Months pass with the AI playing every fixture, my old club's included.
        PlayUntil(engine, new DateOnly(2026, 10, 20));
        Assert.All(state.Competitions.SelectMany(c => c.Fixtures).Where(f => f.Date < state.Date), f => Assert.True(f.Concluded));
        Assert.Contains("Out of work", engine.NextMatchCaption());

        // Still unemployed after a save and load.
        Assert.Equal("Game saved successfully.", engine.SaveGame("unemployed"));
        engine.StartNewGame();
        Assert.Null(engine.LoadGame("unemployed.fms"));
        Assert.True(engine.IsUnemployed);

        var vacancy = state.Clubs.First(c => c.Id != 9);
        vacancy.VacantUntil = state.Date.AddDays(7);
        Assert.True(_host.Get<IJobOfferHelper>().JoinClub(vacancy.Id));
        Assert.False(engine.IsUnemployed);
        Assert.Equal(vacancy.Id, engine.MyClub.Id);
        PlayUntil(engine, state.Date.AddDays(14));
    }

    [Fact]
    public void PlayingAWholeSeason_PromotesAndRelegatesAndStartsTheNextSeason()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var state = engine.State;
        var division1 = state.Clubs.Where(c => c.LeagueId == 1).Select(c => c.Id).ToHashSet();
        var division2 = state.Clubs.Where(c => c.LeagueId == 2).Select(c => c.Id).ToHashSet();
        LeagueModel? finalDivision1 = null, finalDivision2 = null;

        // 1 March: every club promotes 2-4 academy players aged 16 or 17, and I'm told about mine.
        var lastIdBeforeIntake = state.Players.Max(p => p.Id);
        PlayUntil(engine, new DateOnly(2027, 3, 1));
        var intake = state.Players.Where(p => p.Id > lastIdBeforeIntake).ToList();
        Assert.All(state.Clubs, c => Assert.InRange(intake.Count(p => p.Contract?.ClubId == c.Id), 2, 4));
        Assert.All(intake, p => Assert.InRange(p.GetAge(state.Date), 16, 17));
        Assert.All(intake, p => Assert.Contains(state.Clubs.Single(c => c.Id == p.Contract!.ClubId).TacticSlots, s => s.PlayerId == p.Id));
        var youthNotification = Assert.Single(state.Notifications, n => n.Subject == "Youth Intake");
        Assert.All(intake.Where(p => p.Contract!.ClubId == state.MyClubId), p => Assert.Contains(p.Name, youthNotification.Message));

        PlayUntil(engine, new DateOnly(2027, 6, 29));

        // Every competitive fixture of the season has been played.
        Assert.All(state.Competitions.SelectMany(c => c.Fixtures), f => Assert.True(f.Concluded));

        // No club in any league played more than two home or away league games in a row.
        Assert.All(state.Competitions.OfType<LeagueModel>(), l => Assert.True(FixtureRuns.LongestHomeOrAwayRun(l.Fixtures) <= 2, l.Name));
        var cup = (CupModel)state.Competitions.Single(c => c.Type == CompetitionType.Cup);
        Assert.Single(cup.Fixtures, f => f.Round == 7);
        Assert.NotNull(cup.Fixtures.Single(f => f.Round == 7).ClubWon);
        var scotland = (LeagueModel)state.Competitions.Single(c => c.Id == 16);
        Assert.Equal(198 + 30, scotland.Fixtures.Count);
        Assert.Equal(6, scotland.TopSplitClubIds.Count);
        Assert.Single(state.Notifications, n => n.Subject == "Division 1 Split");
        Assert.Single(state.Competitions.Single(c => c.Id == 2).Fixtures, f => f.FixtureType == FixtureType.PlayoffFinal);
        finalDivision1 = (LeagueModel)state.Competitions.Single(c => c.Id == 1);
        finalDivision2 = (LeagueModel)state.Competitions.Single(c => c.Id == 2);
        var relegated = finalDivision1.GenerateLeagueTable().TakeLast(3).Select(p => p.Club.Id).ToList();
        var promoted = finalDivision2.GenerateLeagueTable().Take(2).Select(p => p.Club.Id).ToList();
        promoted.Add(finalDivision2.Fixtures.Single(f => f.FixtureType == FixtureType.PlayoffFinal).ClubWon!.Id);
        var relegatedFromDivision5 = ((LeagueModel)state.Competitions.Single(c => c.Id == 13)).RelegatedClubs().Select(p => p.Id).ToList();
        var promotedFromDivision6 = state.Competitions.OfType<LeagueModel>().Where(l => l.Rank == 6).SelectMany(l => l.PromotedClubs()).Select(p => p.Id).ToList();
        Assert.Equal(4, promotedFromDivision6.Count);

        // The day after each competition's final match, its outcome has been announced.
        foreach (var division in new[] { "Division 1", "Division 2", "Division 3", "Division 4", "Division 5", "Division 6 North", "Division 6 South" })
            Assert.Single(state.Notifications, n => n.Subject == $"England - {division} Promotion & Relegation");
        var division2Review = state.Notifications.Single(n => n.Subject == "England - Division 2 Promotion & Relegation");
        Assert.All(promoted, id => Assert.Contains(state.Clubs.Single(c => c.Id == id).Name, division2Review.Message));
        Assert.Single(state.Notifications, n => n.Subject == $"{cup.Name} Winners" && n.Message.StartsWith(cup.Fixtures.Single(f => f.Round == 7).ClubWon!.Name));
        Assert.Single(state.Notifications, n => n.Subject.EndsWith("Shield Winners"));

        var budgetsBefore = state.Clubs.ToDictionary(c => c.Id, c => c.TransferBudget);

        PlayUntil(engine, new DateOnly(2027, 6, 30)); // end-of-season processing runs on 30 June

        // Every club receives its seasonal budget allocation for its new league on top of what it had left.
        var allocations = SeasonalBudgetRules.Budgets(state.Clubs, _host.Settings.Competitions);
        Assert.All(state.Clubs, c => Assert.Equal(budgetsBefore[c.Id] + allocations[c.Id], c.TransferBudget));

        // Promoted clubs are now paid as top-flight clubs, relegated clubs as second-tier ones.
        var startingBudgets = SeasonalBudgetRules.Budgets(_host.Settings.Clubs, _host.Settings.Competitions);
        Assert.All(promoted, id => Assert.True(allocations[id] > startingBudgets[id]));
        Assert.All(relegated, id => Assert.True(allocations[id] < startingBudgets[id]));

        Assert.All(relegated, id => Assert.Equal(2, state.Clubs.Single(c => c.Id == id).LeagueId));
        Assert.All(promoted, id => Assert.Equal(1, state.Clubs.Single(c => c.Id == id).LeagueId));
        Assert.Equal(20, state.Clubs.Count(c => c.LeagueId == 1));
        Assert.Equal(24, state.Clubs.Count(c => c.LeagueId == 2));

        // Division 5's relegated clubs are split evenly between the two regional Division 6 leagues.
        Assert.Equal(2, relegatedFromDivision5.Count(id => state.Clubs.Single(c => c.Id == id).LeagueId == 14));
        Assert.Equal(2, relegatedFromDivision5.Count(id => state.Clubs.Single(c => c.Id == id).LeagueId == 15));
        Assert.All(promotedFromDivision6, id => Assert.Equal(13, state.Clubs.Single(c => c.Id == id).LeagueId));
        Assert.Equal(24, state.Clubs.Count(c => c.LeagueId == 13));
        Assert.Equal(24, state.Clubs.Count(c => c.LeagueId == 14));
        Assert.Equal(24, state.Clubs.Count(c => c.LeagueId == 15));
        Assert.Contains(state.Notifications, n => n.Subject == "New Season");

        // The new season's competitions are built from the updated divisions.
        var newDivision1 = state.Competitions.OfType<LeagueModel>().Single(l => l.Id == 1);
        Assert.Equal(state.Clubs.Where(c => c.LeagueId == 1).Select(c => c.Id).OrderBy(i => i), newDivision1.Clubs.Select(c => c.Id).OrderBy(i => i));
        Assert.All(newDivision1.Fixtures, f => Assert.False(f.Concluded));
        Assert.All(state.Clubs, c => Assert.InRange(state.Players.Count(p => p.Contract?.ClubId == c.Id), 25, 40));
        Assert.DoesNotContain(state.Players, p => p.SuspensionMatches > 0);

        // A season of transfers, top-ups and youth intakes never leaves two players at a club sharing a number.
        Assert.All(state.Clubs, c =>
        {
            var numbers = state.Players.Where(p => p.Contract?.ClubId == c.Id).Select(p => p.ShirtNumber).ToList();
            Assert.DoesNotContain(null, numbers);
            Assert.Equal(numbers.Count, numbers.Distinct().Count());
        });

        // Out-of-contract players aged 35+ have left the game, and no lineup still points at them.
        Assert.DoesNotContain(state.Players, p => p.Contract == null && p.IsRetiring(state.Date));
        var playerIds = state.Players.Select(p => p.Id).ToHashSet();
        Assert.All(state.Clubs.SelectMany(c => c.TacticSlots), s => Assert.True(s.PlayerId == null || playerIds.Contains(s.PlayerId.Value)));

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

        // Fixtures and competitions point at the real clubs again, not copies.
        var clubs = state.Clubs.ToHashSet();
        Assert.All(state.Competitions.SelectMany(c => c.Clubs), c => Assert.Contains(c, clubs));
        Assert.All(state.Competitions.SelectMany(c => c.Fixtures), f =>
        {
            Assert.Contains(f.HomeClub, clubs);
            Assert.Contains(f.AwayClub, clubs);
            Assert.True(f.ClubWon == null || clubs.Contains(f.ClubWon));
        });

        // Only the club list holds full clubs, so the lineups appear once per club.
        var json = File.ReadAllText(Path.Combine(_host.Storage.SaveDirectory, "midseason.fms"));
        Assert.Equal(state.Clubs.Count, System.Text.RegularExpressions.Regex.Matches(json, "\"TacticSlots\"").Count);

        // The loaded game carries on: the next cup draw still fires.
        PlayUntil(engine, new DateOnly(2026, 9, 5));
        Assert.Contains(state.Competitions.OfType<CupModel>().Single(c => c.Type == CompetitionType.Cup).Fixtures, f => f.Round == 2);
    }

    [Fact]
    public void Autosave_KeepsUpWithTheCareerAndLoadsBackToTheSameDay()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var state = engine.State;
        PlayUntil(engine, new DateOnly(2026, 8, 20)); // through match days as well as quiet ones

        // The hooks have been saving along the way.
        var save = Assert.Single(engine.ListSaves());
        Assert.True(save.IsAutosave);
        Assert.Equal(engine.MyClub.Name, save.ClubName);

        Assert.True(engine.AutoSave());
        var (date, clubId, budget) = (state.Date, state.MyClubId, engine.MyClub.TransferBudget);
        engine.StartNewGame();
        Assert.Null(engine.LoadGame(save.FileName));

        Assert.Equal((date, clubId, budget), (state.Date, state.MyClubId, engine.MyClub.TransferBudget));
        Assert.Equal(save.FileName, engine.AutosaveFileName); // carries on in the same slot
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
    public void NextSeasonsShield_IsLeagueChampionsAgainstCupWinners()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var state = engine.State;
        PlayUntil(engine, new DateOnly(2027, 6, 29));

        var table = ((LeagueModel)state.Competitions.Single(c => c.Id == 1)).GenerateLeagueTable().Select(p => p.Club.Id).ToList();
        var cup = state.Competitions.Single(c => c.Id == 8);
        var cupWinner = cup.Fixtures.Single(f => f.Round == cup.DrawSettings.Max(d => d.Round)).ClubWon!.Id;
        int[] expected = [table[0], cupWinner == table[0] ? table[1] : cupWinner];

        PlayUntil(engine, new DateOnly(2027, 6, 30));

        var shield = state.Competitions.Single(c => c.Type == CompetitionType.EnglishCommunityShield);
        Assert.Equal(expected, shield.Clubs.Select(c => c.Id));

        PlayUntil(engine, shield.DrawSettings.Single().FixtureDate.AddDays(-1));
        var fixture = Assert.Single(shield.Fixtures);
        Assert.Equal(expected, new[] { fixture.HomeClub.Id, fixture.AwayClub.Id });
    }

    [Fact]
    public void RequestHigherTransferBudget_PaysOutAfterTwoDaysAndCannotBeSpammed()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var budget = engine.MyClub.TransferBudget;

        Assert.Equal("Budget request has been submitted.",engine.RequestHigherTransferBudget());
        Assert.Contains("3 months", engine.RequestHigherTransferBudget());

        PlayUntil(engine, engine.State.Date.AddDays(2));

        // Bids for my players are all rejected, so the grant is the only change to my budget.
        Assert.Equal(budget + 10_000_000, engine.MyClub.TransferBudget);
        Assert.Contains(engine.State.Notifications, n => n.Subject == "Budget Request");
    }
}
