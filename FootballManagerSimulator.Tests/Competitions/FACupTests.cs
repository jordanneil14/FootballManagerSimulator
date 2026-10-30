using FootballManagerSimulator.Maui.Competitions.Providers;
using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;
using FootballManagerSimulator.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Tests.Competitions;

/// <summary>
/// The English pyramid at its real sizes: Division 1 (20 clubs), Divisions 2-4 (24 each), Division 5 (24)
/// and Division 6 North and South (24 each), plus a foreign league that must stay out of the cup.
/// </summary>
public class FACupTests : IDisposable
{
    private static readonly (int Id, int Rank, int Clubs)[] EnglishLeagues =
        [(1, 1, 20), (2, 2, 24), (3, 3, 24), (4, 4, 24), (13, 5, 24), (14, 6, 24), (15, 6, 24)];

    private readonly GameHost _host;
    private IState State => _host.State;

    public FACupTests()
    {
        var settings = new SettingsModel
        {
            Competitions =
            [
                .. EnglishLeagues.Select(l => new CompetitionModel { Id = l.Id, CountryId = 1, Rank = l.Rank, IsLeague = true, Type = "EnglishFootballLeague" }),
                new CompetitionModel { Id = 5, CountryId = 2, Rank = 1, IsLeague = true, Type = "League" },
                new CompetitionModel { Id = 8, CountryId = 1, Type = "Cup", Name = "English Cup" },
                new CompetitionModel { Id = 20, CountryId = 1, Type = "EnglishFACup", Name = "National Association Cup" }
            ]
        };
        _host = new GameHost(settings);
        State.Date = new DateOnly(2026, 7, 1);
        State.MyClubId = 1;

        var nextId = 1;
        foreach (var (leagueId, _, clubs) in EnglishLeagues.Append((5, 1, 10)))
            for (var i = 0; i < clubs; i++, nextId++)
                State.Clubs.Add(TestData.Club(nextId, leagueId: leagueId));
    }

    public void Dispose() => _host.Dispose();

    private IEnumerable<ClubModel> ClubsIn(params int[] leagueIds) => State.Clubs.Where(c => leagueIds.Contains(c.LeagueId));

    private CupModel CreateCup()
    {
        var cup = (CupModel)_host.Get<EnglishFACupProvider>().CreateCompetition(
            new CompetitionModel { Id = 20, CountryId = 1, Type = "EnglishFACup", Name = "National Association Cup" });
        State.Competitions.Add(cup);
        return cup;
    }

    private void RunNextDraw()
    {
        var draw = State.Events.OfType<EnglishFACupFixtureDrawEvent>().Where(e => !e.IsCompleted).MinBy(e => e.TriggerDate)!;
        State.Date = draw.TriggerDate;
        draw.Execute();
        draw.IsCompleted = true;
    }

    // The home side wins every tie.
    private static void PlayRound(CupModel cup, int round)
    {
        foreach (var tie in cup.Fixtures.Where(f => f.Round == round))
            (tie.GoalsHome, tie.GoalsAway, tie.Concluded, tie.ClubWon) = (1, 0, true, tie.HomeClub);
    }

    private static HashSet<int> ClubIdsIn(CupModel cup, int round) =>
        cup.Fixtures.Where(f => f.Round == round).SelectMany(f => new[] { f.HomeClub.Id, f.AwayClub.Id }).ToHashSet();

    // ----- Creating the cup -------------------------------------------------------------

    [Fact]
    public void CreateCompetition_TakesEveryEnglishClubButNoForeignOnes()
    {
        var cup = CreateCup();

        Assert.Equal(164, cup.Clubs.Count);
        Assert.DoesNotContain(cup.Clubs, c => c.LeagueId == 5);
        Assert.Equal(CompetitionType.EnglishFACup, cup.Type);
        Assert.Equal(2026, cup.Year);
    }

    [Fact]
    public void CreateCompetition_SchedulesEightSaturdayRoundsAndTheirDraws()
    {
        var cup = CreateCup();

        DateOnly[] expected =
        [
            new(2026, 11, 7), new(2026, 12, 5), new(2027, 1, 2), new(2027, 1, 30),
            new(2027, 2, 27), new(2027, 3, 20), new(2027, 4, 10), new(2027, 5, 15)
        ];
        Assert.Equal(expected, cup.DrawSettings.Select(d => d.FixtureDate));
        Assert.Equal(Enumerable.Range(1, 8), cup.DrawSettings.Select(d => d.Round));
        Assert.Equal(new DateOnly(2026, 10, 24), cup.DrawSettings[0].DrawDate);
        for (var i = 1; i < 8; i++)
            Assert.Equal(cup.DrawSettings[i - 1].FixtureDate.AddDays(1), cup.DrawSettings[i].DrawDate);
        Assert.Equal(cup.DrawSettings.Select(d => d.DrawDate), State.Events.OfType<EnglishFACupFixtureDrawEvent>().Select(e => e.TriggerDate));
    }

    [Fact]
    public void CreateCompetition_MovesARoundBackAWeekWhenItLandsOnALeagueCupDate()
    {
        State.Competitions.Add(new CupModel { Id = 8, Type = CompetitionType.Cup, DrawSettings = [new DrawSettingsModel { Round = 7, FixtureDate = new DateOnly(2026, 11, 7) }] });

        var cup = CreateCup();

        Assert.Equal(new DateOnly(2026, 11, 14), cup.DrawSettings[0].FixtureDate);
        Assert.Equal(new DateOnly(2026, 10, 31), cup.DrawSettings[0].DrawDate);
        Assert.Equal(new DateOnly(2026, 12, 5), cup.DrawSettings[1].FixtureDate);
    }

    [Fact]
    public void CreateCompetition_AvoidsTheLeagueCupFinalIn2029()
    {
        State.Date = new DateOnly(2029, 7, 1);
        var leagueCup = (CupModel)_host.Get<EnglishLeagueCupProvider>().CreateCompetition(new CompetitionModel { Id = 8, Name = "English Cup", Type = "Cup" });
        State.Competitions.Add(leagueCup);
        Assert.Equal(new DateOnly(2029, 11, 3), leagueCup.DrawSettings[^1].FixtureDate); // the first Saturday of November

        var cup = CreateCup();

        Assert.Equal(new DateOnly(2029, 11, 10), cup.DrawSettings[0].FixtureDate);
    }

    // ----- Round 1 ------------------------------------------------------------------------

    [Fact]
    public void RoundOne_IsDivisions3And4WithThirtyTwoNonLeagueClubsDrawnAtRandom()
    {
        var cup = CreateCup();

        RunNextDraw();

        var ties = cup.Fixtures.Where(f => f.Round == 1).ToList();
        Assert.Equal(40, ties.Count);
        var clubIds = ClubIdsIn(cup, 1);
        Assert.Equal(80, clubIds.Count);
        Assert.All(ClubsIn(3, 4), c => Assert.Contains(c.Id, clubIds));
        Assert.Equal(32, ClubsIn(13, 14, 15).Count(c => clubIds.Contains(c.Id)));
        Assert.DoesNotContain(ClubsIn(1, 2, 5), c => clubIds.Contains(c.Id));
        Assert.All(ties, t => Assert.Equal((new DateOnly(2026, 11, 7), new TimeOnly(15, 0)), (t.Date, t.KickOffTime)));
    }

    [Fact]
    public void RoundOne_AlwaysIncludesMyNonLeagueClub()
    {
        foreach (var myClub in new[] { ClubsIn(13).First(), ClubsIn(14).Last(), ClubsIn(15).First() })
        {
            for (var season = 0; season < 5; season++)
            {
                State.Competitions.Clear();
                State.Events.Clear();
                State.MyClubId = myClub.Id;
                var cup = CreateCup();

                RunNextDraw();

                Assert.Contains(myClub.Id, ClubIdsIn(cup, 1));
                Assert.Equal(80, ClubIdsIn(cup, 1).Count);
            }
        }
    }

    [Fact]
    public void RoundOne_DrawsDifferentNonLeagueClubsEachSeason()
    {
        var draws = new HashSet<string>();
        for (var season = 0; season < 5; season++)
        {
            State.Competitions.Clear();
            State.Events.Clear();
            var cup = CreateCup();
            RunNextDraw();
            draws.Add(string.Join(",", ClubIdsIn(cup, 1).Order()));
        }

        Assert.True(draws.Count > 1);
    }

    [Fact]
    public void Draw_TellsMeTheTiesWithMineFirst()
    {
        var myClub = ClubsIn(4).Last();
        State.MyClubId = myClub.Id;
        var cup = CreateCup();

        RunNextDraw();

        var notification = Assert.Single(State.Notifications, n => n.Subject == "National Association Cup Round 1");
        var lines = notification.Message.Split('\n');
        Assert.Equal("The draw has been made for round 1 of the National Association Cup, to be played on 7 November 2026:", lines[0]);
        Assert.Contains(myClub.Name, lines[1]);
        Assert.Equal(41, lines.Length);
    }

    // ----- The whole cup ------------------------------------------------------------------

    [Fact]
    public void TheCup_RunsFromEightyClubsToAFinal_WithDivisions1And2JoiningInRoundThree()
    {
        var cup = CreateCup();

        for (var round = 1; round <= FACupRules.Rounds; round++)
        {
            RunNextDraw();
            Assert.All(cup.Fixtures.Where(f => f.Round == round), f => Assert.Equal(cup.DrawSettings[round - 1].FixtureDate, f.Date));
            PlayRound(cup, round);
        }

        Assert.Equal([40, 20, 32, 16, 8, 4, 2, 1], Enumerable.Range(1, 8).Select(r => cup.Fixtures.Count(f => f.Round == r)));

        var roundTwoWinners = cup.Fixtures.Where(f => f.Round == 2).Select(f => f.ClubWon!.Id);
        var roundThree = ClubIdsIn(cup, 3);
        Assert.Equal(64, roundThree.Count);
        Assert.All(ClubsIn(1, 2), c => Assert.Contains(c.Id, roundThree));
        Assert.All(roundTwoWinners, id => Assert.Contains(id, roundThree));
        Assert.DoesNotContain(ClubsIn(1, 2), c => ClubIdsIn(cup, 1).Contains(c.Id) || ClubIdsIn(cup, 2).Contains(c.Id));

        for (var round = 2; round <= FACupRules.Rounds; round++)
        {
            var previousWinners = cup.Fixtures.Where(f => f.Round == round - 1).Select(f => f.ClubWon!.Id).ToHashSet();
            Assert.Subset(previousWinners.Concat(round == 3 ? ClubsIn(1, 2).Select(c => c.Id) : []).ToHashSet(), ClubIdsIn(cup, round));
        }

        Assert.Equal(new DateOnly(2027, 5, 15), cup.Fixtures.Single(f => f.Round == 8).Date);
        Assert.Equal(FACupRules.Rounds + 1, cup.Round);
    }

    [Fact]
    public void Draw_DoesNothingWithoutTheCup()
    {
        var draw = ActivatorUtilities.CreateInstance<EnglishFACupFixtureDrawEvent>(_host.Services, State.Date, false);

        draw.Execute();

        Assert.Empty(State.Notifications);
    }

    [Fact]
    public void Service_RefusesToDrawAnOddNumberOfClubs()
    {
        var clubs = State.Clubs.Take(3).ToList();
        var cup = new CupModel
        {
            Name = "National Association Cup",
            Type = CompetitionType.EnglishFACup,
            Clubs = clubs,
            DrawSettings = [new DrawSettingsModel { Round = 1, IntroducedClubIds = clubs.Select(c => c.Id).ToList() }]
        };

        Assert.Throws<InvalidOperationException>(() => _host.Get<EnglishFACupService>().GenerateNextRoundOfFixtures(cup));
    }

    [Fact]
    public void Service_SendsAPreMatchReportNamingTheCup()
    {
        var cup = CreateCup();
        State.MyClubId = ClubsIn(3).First().Id;
        RunNextDraw();
        var mine = cup.Fixtures.Single(f => f.HomeClub.Id == State.MyClubId || f.AwayClub.Id == State.MyClubId);

        _host.Get<EnglishFACupService>().GeneratePreMatchReportForFixture(mine);

        Assert.Contains(State.Notifications, n => n.Subject == "Pre-Match Report" && n.Message.Contains("upcoming National Association Cup tie"));
    }

    // ----- Rules --------------------------------------------------------------------------

    [Theory]
    [InlineData(1, 25_000)]
    [InlineData(2, 45_000)]
    [InlineData(3, 65_000)]
    [InlineData(4, 95_000)]
    [InlineData(5, 150_000)]
    [InlineData(6, 300_000)]
    [InlineData(7, 600_000)]
    [InlineData(8, 1_250_000)]
    public void PrizeMoney_PaysBothClubsTheWinnerDouble(int round, int loser)
    {
        Assert.Equal(loser, FACupRules.ForLoser(round));
        Assert.Equal(2 * loser, FACupRules.ForWinner(round));
    }

    [Fact]
    public void PlannedDates_AreSaturdaysInOrderWithTheFinalAfterThePlayoffs()
    {
        for (var year = 2026; year <= 2060; year++)
        {
            var dates = Enumerable.Range(1, FACupRules.Rounds).Select(r => FACupRules.PlannedDate(r, year)).ToList();
            Assert.All(dates, d => Assert.Equal(DayOfWeek.Saturday, d.DayOfWeek));
            Assert.Equal(dates.Order(), dates);
            Assert.True(dates[6] < new DateOnly(year + 1, 4, 22), "the semi-final is before the leagues end");
            Assert.True(dates[7] > new DateOnly(year + 1, 5, 7), "the final is after the play-off finals");
        }
    }
}

/// <summary>Who plays in next season's Community Shield, on the real game world.</summary>
[Collection(SaveFilesCollection.Name)]
public class CommunityShieldQualificationTests : IDisposable
{
    private readonly GameHost _host = new();

    public void Dispose() => _host.Dispose();

    // Club A tops Division 1; the League Cup and the FA Cup are won by two other clubs.
    private (int Champions, int LeagueCupWinners, int FACupWinners) SetUpTheSeason(GameEngine engine)
    {
        var state = engine.State;
        var division1 = (LeagueModel)state.Competitions.Single(c => c.Id == 1);
        var (a, b, c, d) = (division1.Clubs[0], division1.Clubs[1], division1.Clubs[2], division1.Clubs[3]);
        division1.Fixtures.Add(TestData.Result(a, d, 5, 0));

        var leagueCup = state.Competitions.Single(x => x.Type == CompetitionType.Cup);
        leagueCup.Fixtures.Add(new FixtureModel { HomeClub = b, AwayClub = d, Round = 7, Concluded = true, ClubWon = b, GoalsHome = 1, GoalsAway = 0 });

        var faCup = state.Competitions.Single(x => x.Type == CompetitionType.EnglishFACup);
        faCup.Fixtures.Add(new FixtureModel { HomeClub = c, AwayClub = d, Round = FACupRules.Rounds, Concluded = true, ClubWon = c, GoalsHome = 1, GoalsAway = 0 });

        // The season is skipped, so settle each play-off to keep the divisions the right size.
        foreach (var league in state.Competitions.OfType<LeagueModel>().Where(l => l.CountryId == 1 && l.Rank > 1 && l.HasPlayoffs))
        {
            var table = league.GenerateLeagueTable().Select(p => p.Club).ToList();
            var winner = table[league.AutomaticPromotionPlaces];
            var final = TestData.Result(winner, table[^1], 1, 0, FixtureType.PlayoffFinal);
            final.ClubWon = winner;
            league.Fixtures.Add(final);
        }

        return (a.Id, b.Id, c.Id);
    }

    private void EndTheSeason(GameEngine engine)
    {
        engine.State.Date = new DateOnly(2027, 6, 29);
        _host.Get<IProcessHelper>().Process();
    }

    private int[] ShieldClubIds(GameEngine engine) =>
        engine.State.Competitions.Single(c => c.Type == CompetitionType.EnglishCommunityShield).Clubs.Select(c => c.Id).ToArray();

    [Fact]
    public void TheShieldIsTheChampionsAgainstTheFACupWinners()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var (champions, _, faCupWinners) = SetUpTheSeason(engine);

        EndTheSeason(engine);

        Assert.Equal([champions, faCupWinners], ShieldClubIds(engine));
    }

    [Fact]
    public void WithoutAnFACupTheShieldFallsBackToTheLeagueCupWinners()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var (champions, leagueCupWinners, _) = SetUpTheSeason(engine);
        engine.State.Competitions.RemoveAll(c => c.Type == CompetitionType.EnglishFACup); // a save from before the FA Cup

        EndTheSeason(engine);

        Assert.Equal([champions, leagueCupWinners], ShieldClubIds(engine));
    }
}
