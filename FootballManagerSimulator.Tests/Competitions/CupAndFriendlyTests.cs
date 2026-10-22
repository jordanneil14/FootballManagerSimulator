using FootballManagerSimulator.Maui.Competitions.Providers;
using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Tests.Competitions;

public class EnglishLeagueCupServiceTests
{
    private readonly StateModel _state = new() { Date = new DateOnly(2026, 7, 13), MyClubId = 1 };
    private readonly SettingsModel _settings = new()
    {
        Competitions =
        [
            new CompetitionModel { Id = 1, Type = nameof(CompetitionType.EnglishPremierLeague), IsLeague = true },
            new CompetitionModel { Id = 2, Type = nameof(CompetitionType.EnglishFootballLeague), IsLeague = true },
            new CompetitionModel { Id = 3, Type = nameof(CompetitionType.EnglishFootballLeague), IsLeague = true },
        ]
    };
    private readonly EnglishLeagueCupService _service;

    public EnglishLeagueCupServiceTests() => _service = new EnglishLeagueCupService(Options.Create(_settings), new RecordingNotifications(), _state);

    private CupModel Cup(IEnumerable<ClubModel> clubs) => new()
    {
        Clubs = clubs.ToList(),
        DrawSettings =
        [
            new DrawSettingsModel { Round = 1, FixtureDate = new DateOnly(2026, 8, 4) },
            new DrawSettingsModel { Round = 2, FixtureDate = new DateOnly(2026, 8, 18), IntroducedClubIds = [1, 2] }
        ]
    };

    [Fact]
    public void RoundOne_DrawsOnlyFootballLeagueClubsIntoDistinctPairs()
    {
        var premier = Enumerable.Range(1, 4).Select(i => TestData.Club(i, leagueId: 1));
        var efl = Enumerable.Range(10, 7).Select(i => TestData.Club(i, leagueId: i < 14 ? 2 : 3)); // 7 clubs -> one gets a bye
        var cup = Cup(premier.Concat(efl));

        _service.GenerateNextRoundOfFixtures(cup);

        Assert.Equal(3, cup.Fixtures.Count);
        var ids = cup.Fixtures.SelectMany(f => new[] { f.HomeClub.Id, f.AwayClub.Id }).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(ids, id => Assert.True(id >= 10));
        Assert.All(cup.Fixtures, f =>
        {
            Assert.Equal(1, f.Round);
            Assert.Equal(new DateOnly(2026, 8, 4), f.Date);
            Assert.Equal(new TimeOnly(19, 45), f.KickOffTime);
        });
    }

    [Fact]
    public void LaterRounds_DrawPreviousWinnersPlusIntroducedClubs()
    {
        var clubs = Enumerable.Range(1, 6).Select(i => TestData.Club(i, leagueId: i <= 2 ? 1 : 2)).ToList();
        var cup = Cup(clubs);
        cup.Fixtures.Add(new FixtureModel { HomeClub = clubs[2], AwayClub = clubs[3], Round = 1, Concluded = true, ClubWon = clubs[2] });
        cup.Fixtures.Add(new FixtureModel { HomeClub = clubs[4], AwayClub = clubs[5], Round = 1, Concluded = true, ClubWon = clubs[5] });
        cup.Round = 2;

        _service.GenerateNextRoundOfFixtures(cup);

        var roundTwo = cup.Fixtures.Where(f => f.Round == 2).ToList();
        Assert.Equal(2, roundTwo.Count);
        var drawn = roundTwo.SelectMany(f => new[] { f.HomeClub.Id, f.AwayClub.Id }).OrderBy(i => i);
        Assert.Equal([1, 2, 3, 6], drawn);
        Assert.Equal(4, cup.Fixtures.Count); // round one is kept
    }
}

public class FriendlyServiceTests
{
    [Fact]
    public void GenerateNextRoundOfFixtures_PairsEveryClubAndTellsMeWhoIPlay()
    {
        var state = TestData.StateWithClubs(6, myClubId: 4);
        var notifications = new RecordingNotifications { Now = state.Date };
        var service = new FriendlyService(notifications, state);
        var friendly = new FriendlyModel
        {
            Clubs = state.Clubs.ToList(),
            DrawSettings = [new DrawSettingsModel { Round = 1, FixtureDate = new DateOnly(2026, 7, 11) }, new DrawSettingsModel { Round = 2, FixtureDate = new DateOnly(2026, 7, 18) }]
        };

        service.GenerateNextRoundOfFixtures(friendly);
        service.GenerateNextRoundOfFixtures(friendly);

        Assert.Equal(2, friendly.Round);
        Assert.Equal(6, friendly.Fixtures.Count);
        foreach (var round in friendly.Fixtures.GroupBy(f => f.Round))
        {
            var ids = round.SelectMany(f => new[] { f.HomeClub.Id, f.AwayClub.Id }).ToList();
            Assert.Equal(Enumerable.Range(1, 6), ids.OrderBy(i => i));
        }
        Assert.All(notifications.Sent, n => Assert.Equal("Friendly Arranged", n.Subject));
        Assert.Equal(2, notifications.Sent.Count);
    }

    [Fact]
    public void GenerateNextRoundOfFixtures_WhileUnemployedStillDrawsButTellsMeNothing()
    {
        var state = TestData.StateWithClubs(4);
        state.MyClubId = null;
        var notifications = new RecordingNotifications { Now = state.Date };
        var friendly = new FriendlyModel
        {
            Clubs = state.Clubs.ToList(),
            DrawSettings = [new DrawSettingsModel { Round = 1, FixtureDate = new DateOnly(2026, 7, 11) }]
        };

        new FriendlyService(notifications, state).GenerateNextRoundOfFixtures(friendly);

        Assert.Equal(2, friendly.Fixtures.Count);
        Assert.Empty(notifications.Sent);
    }

    [Fact]
    public void PreMatchReport_NamesTheOpposition()
    {
        var state = TestData.StateWithClubs(2, myClubId: 2);
        var notifications = new RecordingNotifications();

        new FriendlyService(notifications, state).GeneratePreMatchReportForFixture(new FixtureModel { HomeClub = state.Clubs[0], AwayClub = state.Clubs[1] });

        Assert.Contains("Club 1", Assert.Single(notifications.Sent).Message);
    }
}

public class EnglishCommunityShieldServiceTests
{
    [Fact]
    public void GenerateNextRoundOfFixtures_CreatesASingleFixtureBetweenTheTwoClubs()
    {
        var state = TestData.StateWithClubs(2);
        var cup = new CupModel
        {
            Type = CompetitionType.EnglishCommunityShield,
            Clubs = state.Clubs.ToList(),
            DrawSettings = [new DrawSettingsModel { Round = 1, FixtureDate = new DateOnly(2026, 8, 8) }]
        };

        new EnglishCommunityShieldService(state, new RecordingNotifications()).GenerateNextRoundOfFixtures(cup);

        var fixture = Assert.Single(cup.Fixtures);
        Assert.Equal((1, 2, new DateOnly(2026, 8, 8)), (fixture.HomeClub.Id, fixture.AwayClub.Id, fixture.Date));
    }
}

public class CompetitionProviderTests : IDisposable
{
    private readonly GameHost _host = new(new SettingsModel
    {
        Competitions = [new CompetitionModel { Id = 1, CountryId = 1, IsLeague = true, Type = nameof(CompetitionType.EnglishPremierLeague) }]
    });

    public CompetitionProviderTests()
    {
        _host.State.Date = new DateOnly(2026, 7, 1);
        _host.State.Clubs.AddRange(Enumerable.Range(1, 20).Select(i => TestData.Club(i, leagueId: 1)));
        _host.State.Clubs.Add(TestData.Club(99, leagueId: 50)); // a foreign club
    }

    public void Dispose() => _host.Dispose();

    private static CompetitionModel LeagueSettings(int id, string type) => new()
    {
        Id = id,
        Name = "Division 1",
        Type = type,
        Rank = 1,
        CountryId = 1,
        IsLeague = true,
        LeagueTable = new CompetitionModel.LeagueTableModel { AutomaticPromotionPlaces = 1, PlayoffPlaces = 4, RelegationPlaces = 3 }
    };

    [Fact]
    public void LeagueProvider_CreatesLeagueFromClubsInThatLeagueWithFixtures()
    {
        var provider = ActivatorUtilities.CreateInstance<LeagueProvider>(_host.Services);

        var league = Assert.IsType<LeagueModel>(provider.CreateCompetition(LeagueSettings(1, "League")));

        Assert.Equal(20, league.Clubs.Count);
        Assert.DoesNotContain(league.Clubs, c => c.Id == 99);
        Assert.Equal((1, 4, 3, 2026), (league.AutomaticPromotionPlaces, league.PlayoffPlaces, league.RelegationPlaces, league.Year));
        Assert.Equal(380, league.Fixtures.Count);
    }

    [Fact]
    public void LeagueProvider_ThrowsWhenTheLeagueHasNoClubs()
    {
        var provider = ActivatorUtilities.CreateInstance<LeagueProvider>(_host.Services);
        Assert.Throws<Exception>(() => provider.CreateCompetition(LeagueSettings(42, "League")));
    }

    [Fact]
    public void ProvidersAreRegisteredForEveryCompetitionType()
    {
        var types = _host.Get<IEnumerable<ICompetitionProvider>>().Select(p => p.Type).OrderBy(t => t);
        Assert.Equal(Enum.GetValues<CompetitionType>().OrderBy(t => t), types);
    }

    [Fact]
    public void EnglishLeagueCupProvider_SchedulesSevenRoundsWithADrawEventEach()
    {
        var provider = _host.Get<EnglishLeagueCupProvider>();

        var cup = Assert.IsType<CupModel>(provider.CreateCompetition(new CompetitionModel { Id = 8, Name = "Cup", Type = "Cup" }));

        Assert.Equal(20, cup.Clubs.Count); // only clubs in country-1 leagues
        Assert.Equal(Enumerable.Range(1, 7), cup.DrawSettings.Select(d => d.Round));
        var roundOne = new DateOnly(2026, 8, 4); // first Tuesday on/after 1 August 2026
        Assert.Equal(roundOne, cup.DrawSettings[0].FixtureDate);
        Assert.Equal(roundOne.AddDays(14), cup.DrawSettings[1].FixtureDate);
        Assert.Equal(roundOne.AddDays(5 * 14), cup.DrawSettings[5].FixtureDate);
        Assert.Equal(roundOne.AddDays(6 * 14 + 4), cup.DrawSettings[6].FixtureDate); // final is on a Saturday
        Assert.Equal(DayOfWeek.Saturday, cup.DrawSettings[6].FixtureDate.DayOfWeek);
        Assert.Equal(new DateOnly(2026, 7, 13), cup.DrawSettings[0].DrawDate);
        for (var i = 1; i < 7; i++)
            Assert.Equal(cup.DrawSettings[i - 1].FixtureDate.AddDays(1), cup.DrawSettings[i].DrawDate);
        Assert.Equal(7, _host.State.Events.OfType<EnglishLeagueCupFixtureDrawEvent>().Count());
    }

    [Fact]
    public void EnglishLeagueCupProvider_IntroducesBottomTwelvePremierLeagueClubsInRoundTwoAndTheRestInRoundThree()
    {
        var premierLeague = TestData.League(1, _host.State.Clubs.Where(c => c.LeagueId == 1));
        // Club n beats every club with a higher id, so the table is ordered 1..20.
        foreach (var home in premierLeague.Clubs)
            foreach (var away in premierLeague.Clubs.Where(a => a.Id > home.Id))
                premierLeague.Fixtures.Add(TestData.Result(home, away, 1, 0));
        _host.State.Competitions.Add(premierLeague);
        var provider = _host.Get<EnglishLeagueCupProvider>();
        var cup = new CupModel
        {
            DrawSettings = [new() { Round = 1 }, new() { Round = 2 }, new() { Round = 3 }]
        };

        cup.Round = 2;
        provider.PopulateIntroducedClubs(cup);
        cup.Round = 3;
        provider.PopulateIntroducedClubs(cup);

        Assert.Equal(Enumerable.Range(9, 12), cup.DrawSettings[1].IntroducedClubIds.OrderBy(i => i));
        Assert.Equal(Enumerable.Range(1, 8), cup.DrawSettings[2].IntroducedClubIds.OrderBy(i => i));
        Assert.Empty(cup.DrawSettings[0].IntroducedClubIds);
    }

    [Fact]
    public void EnglishCommunityShieldProvider_PlaysOnTheSaturdayAWeekBeforeTheLeagueStarts()
    {
        var premierLeague = TestData.League(1, _host.State.Clubs.Take(2));
        premierLeague.Fixtures.Add(new FixtureModel { Date = new DateOnly(2026, 8, 15) });
        premierLeague.Fixtures.Add(new FixtureModel { Date = new DateOnly(2026, 8, 18) });
        _host.State.Competitions.Add(premierLeague);
        var provider = ActivatorUtilities.CreateInstance<EnglishCommunityShieldProvider>(_host.Services);
        var settings = new CompetitionModel
        {
            Id = 12,
            Name = "Shield",
            DrawSettings = [new CompetitionModel.DrawSettingsModel { Round = 1, IntroducedClubIds = [3, 4] }]
        };

        var shield = Assert.IsType<CupModel>(provider.CreateCompetition(settings));

        Assert.Equal(CompetitionType.EnglishCommunityShield, shield.Type);
        Assert.Equal([3, 4], shield.Clubs.Select(c => c.Id));
        Assert.Equal(new DateOnly(2026, 8, 8), shield.DrawSettings.Single().FixtureDate);
        Assert.Single(_host.State.Events.OfType<EnglishCommunityShieldFixtureDrawEvent>());
    }

    [Fact]
    public void FriendlyProvider_MovesDrawDatesIntoTheCurrentYear()
    {
        _host.State.Date = new DateOnly(2031, 7, 1);
        var provider = ActivatorUtilities.CreateInstance<FriendlyProvider>(_host.Services);

        var friendly = provider.CreateCompetition(new CompetitionModel
        {
            Id = 9,
            DrawSettings = [new CompetitionModel.DrawSettingsModel { Round = 1, FixtureDate = new DateOnly(2026, 7, 11) }]
        });

        Assert.Equal(new DateOnly(2031, 7, 11), friendly.DrawSettings.Single().FixtureDate);
        Assert.Equal(_host.State.Clubs.Count, friendly.Clubs.Count);
    }
}
