using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Tests.Events;

/// <summary>
/// An eight-club league whose table finishes Club 1 to Club 8 in order, so each play-off pairing is
/// known in advance. The season ends on Monday 3 May 2027: the next Saturday is the 8th, the
/// following Tuesday the 11th.
/// </summary>
public class PlayoffEventTests : IDisposable
{
    private const int LastLeagueRound = 14;
    private static readonly DateOnly SeasonEnd = new(2027, 5, 3);
    private static readonly DateOnly Saturday = new(2027, 5, 8);
    private static readonly DateOnly Tuesday = new(2027, 5, 11);

    private readonly GameHost _host = new(new SettingsModel());
    private readonly ClubModel[] _clubs = Enumerable.Range(1, 8).Select(i => TestData.Club(i)).ToArray();
    private readonly LeagueModel _league;

    private IState State => _host.State;

    public PlayoffEventTests()
    {
        State.Date = SeasonEnd;
        State.Clubs.AddRange(_clubs);

        _league = TestData.League(1, _clubs);
        foreach (var home in _clubs)
            foreach (var away in _clubs.Where(c => c.Id > home.Id))
                _league.Fixtures.Add(WithRound(TestData.Result(home, away, 1, 0), LastLeagueRound));

        State.Competitions.Add(_league);
    }

    public void Dispose() => _host.Dispose();

    private ClubModel Club(int position) => _clubs[position - 1];

    private static FixtureModel WithRound(FixtureModel fixture, int round)
    {
        fixture.Round = round;
        return fixture;
    }

    private void Run<TEvent>() where TEvent : IEvent =>
        ActivatorUtilities.CreateInstance<TEvent>(_host.Services, State.Date, false, _league.Id).Execute();

    private List<FixtureModel> Fixtures(FixtureType type) =>
        _league.Fixtures.Where(f => f.FixtureType == type).ToList();

    private static void Play(FixtureModel fixture, int goalsHome, int goalsAway, ClubModel? winner = null)
    {
        fixture.GoalsHome = goalsHome;
        fixture.GoalsAway = goalsAway;
        fixture.Concluded = true;
        fixture.ClubWon = winner;
    }

    private static (int home, int away) Ids(FixtureModel fixture) => (fixture.HomeClub.Id, fixture.AwayClub.Id);

    // ----- English Football League: two-legged semi-finals ----------------------------------

    private void UseEnglishFootballLeagueFormat()
    {
        _league.AutomaticPromotionPlaces = 2;
        _league.PlayoffPlaces = 4;
    }

    [Fact]
    public void EflSemiFinals_ThirdPlaysSixthAndFourthPlaysFifth_OverTwoLegs()
    {
        UseEnglishFootballLeagueFormat();

        Run<EnglishFootballLeaguePlayoffEvent>();

        var firstLegs = Fixtures(FixtureType.Playoff);
        var secondLegs = Fixtures(FixtureType.PlayoffSecondLeg);
        Assert.Equal([(3, 6), (4, 5)], firstLegs.Select(Ids));
        Assert.Equal([(6, 3), (5, 4)], secondLegs.Select(Ids));
        Assert.All(firstLegs, f => Assert.Equal((Saturday, new TimeOnly(15, 0)), (f.Date, f.KickOffTime)));
        Assert.All(secondLegs, f => Assert.Equal((Tuesday, new TimeOnly(19, 45)), (f.Date, f.KickOffTime)));
        Assert.All(firstLegs.Concat(secondLegs), f => Assert.Equal(LastLeagueRound + 1, f.Round));
    }

    [Fact]
    public void EflSemiFinals_AreAnnouncedAndTheFinalIsDrawnTheDayAfterTheSecondLegs()
    {
        UseEnglishFootballLeagueFormat();

        Run<EnglishFootballLeaguePlayoffEvent>();

        var notification = Assert.Single(State.Notifications, n => n.Subject == "League 1 Playoffs");
        Assert.Contains("Club 3 v Club 6\nClub 4 v Club 5", notification.Message);
        Assert.Contains("1st Leg: 8 May 2027", notification.Message);
        Assert.Contains("2nd Leg: 11 May 2027", notification.Message);
        var final = Assert.Single(State.Events.OfType<EnglishFootballLeaguePlayoffFinalEvent>());
        Assert.Equal(Tuesday.AddDays(1), final.TriggerDate);
    }

    [Fact]
    public void EflSemiFinals_OnASaturdayArePlayedThatDay()
    {
        UseEnglishFootballLeagueFormat();
        State.Date = Saturday;

        Run<EnglishFootballLeaguePlayoffEvent>();

        Assert.All(Fixtures(FixtureType.Playoff), f => Assert.Equal(Saturday, f.Date));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void EflSemiFinals_NeedAtLeastFourPlayoffPlaces(int playoffPlaces)
    {
        _league.AutomaticPromotionPlaces = 2;
        _league.PlayoffPlaces = playoffPlaces;

        Run<EnglishFootballLeaguePlayoffEvent>();

        Assert.Empty(Fixtures(FixtureType.Playoff));
        Assert.Empty(State.Notifications);
        Assert.Empty(State.Events);
    }

    [Fact]
    public void EflSemiFinals_AreSkippedWhenTooFewClubsAreLeftBelowTheAutomaticPlaces()
    {
        _league.AutomaticPromotionPlaces = 5;
        _league.PlayoffPlaces = 4;

        Run<EnglishFootballLeaguePlayoffEvent>();

        Assert.Empty(Fixtures(FixtureType.Playoff));
    }

    private void PlaySemiFinals(
        (int home, int away, ClubModel? winner) first1, (int home, int away, ClubModel? winner) second1,
        (int home, int away, ClubModel? winner) first2, (int home, int away, ClubModel? winner) second2)
    {
        UseEnglishFootballLeagueFormat();
        Run<EnglishFootballLeaguePlayoffEvent>();
        var firstLegs = Fixtures(FixtureType.Playoff);
        var secondLegs = Fixtures(FixtureType.PlayoffSecondLeg);
        Play(firstLegs[0], first1.home, first1.away, first1.winner);
        Play(secondLegs[0], second1.home, second1.away, second1.winner);
        Play(firstLegs[1], first2.home, first2.away, first2.winner);
        Play(secondLegs[1], second2.home, second2.away, second2.winner);
        State.Date = Tuesday.AddDays(1);
        State.Notifications.Clear();
    }

    [Fact]
    public void EflFinal_IsBetweenTheTwoSecondLegWinnersOnTheNextSaturday()
    {
        PlaySemiFinals((1, 0, Club(3)), (2, 0, Club(6)), (0, 0, null), (0, 1, Club(4)));

        Run<EnglishFootballLeaguePlayoffFinalEvent>();

        var final = Assert.Single(Fixtures(FixtureType.PlayoffFinal));
        Assert.Equal((6, 4), Ids(final));
        Assert.Equal((new DateOnly(2027, 5, 15), new TimeOnly(15, 0), LastLeagueRound + 2), (final.Date, final.KickOffTime, final.Round));
        var notification = Assert.Single(State.Notifications);
        Assert.Equal("League 1 Playoff Final", notification.Subject);
        Assert.Contains("Club 6 v Club 4", notification.Message);
        Assert.Contains("15 May 2027", notification.Message);
    }

    [Fact]
    public void EflFinal_SchedulesTheEndOfTheSeasonForTheDayAfter()
    {
        PlaySemiFinals((1, 0, Club(3)), (2, 0, Club(6)), (0, 0, null), (0, 1, Club(4)));

        Run<EnglishFootballLeaguePlayoffFinalEvent>();

        var end = Assert.Single(State.Events.OfType<CompetitionEndEvent>());
        Assert.Equal(new DateOnly(2027, 5, 16), end.TriggerDate);
    }

    [Fact]
    public void EflFinal_FallsBackToTheAggregateWhenASecondLegHasNoWinnerRecorded()
    {
        // Club 3 win 3-1 on aggregate; Club 5 win 2-1 on aggregate.
        PlaySemiFinals((2, 0, Club(3)), (1, 1, null), (1, 0, Club(4)), (2, 0, null));

        Run<EnglishFootballLeaguePlayoffFinalEvent>();

        Assert.Equal((3, 5), Ids(Assert.Single(Fixtures(FixtureType.PlayoffFinal))));
    }

    [Fact]
    public void EflFinal_WaitsUntilBothSecondLegsHaveBeenPlayed()
    {
        PlaySemiFinals((1, 0, Club(3)), (2, 0, Club(6)), (0, 0, null), (0, 1, Club(4)));
        Fixtures(FixtureType.PlayoffSecondLeg)[1].Concluded = false;

        Run<EnglishFootballLeaguePlayoffFinalEvent>();

        Assert.Empty(Fixtures(FixtureType.PlayoffFinal));
        Assert.Empty(State.Notifications);
    }

    // ----- National League: eliminators, semi-finals and a final ----------------------------

    private void UseNationalLeagueFormat()
    {
        _league.AutomaticPromotionPlaces = 1;
        _league.PlayoffPlaces = 6;
    }

    [Fact]
    public void NationalEliminators_FifthPlaysSixthAndFourthPlaysSeventhOnTheNextSaturday()
    {
        UseNationalLeagueFormat();

        Run<NationalLeaguePlayoffQuarterFinalEvent>();

        var eliminators = Fixtures(FixtureType.Playoff);
        Assert.Equal([(5, 6), (4, 7)], eliminators.Select(Ids));
        Assert.All(eliminators, f => Assert.Equal((Saturday, new TimeOnly(15, 0), LastLeagueRound + 1), (f.Date, f.KickOffTime, f.Round)));
        var notification = Assert.Single(State.Notifications, n => n.Subject == "League 1 Playoffs");
        Assert.Contains("Club 5 v Club 6\nClub 4 v Club 7", notification.Message);
        var semis = Assert.Single(State.Events.OfType<NationalLeaguePlayoffSemiFinalEvent>());
        Assert.Equal(Saturday.AddDays(1), semis.TriggerDate);
    }

    [Theory]
    [InlineData(1, 4)]
    [InlineData(3, 6)]
    public void NationalEliminators_NeedSixPlayoffClubs(int automaticPlaces, int playoffPlaces)
    {
        _league.AutomaticPromotionPlaces = automaticPlaces;
        _league.PlayoffPlaces = playoffPlaces;

        Run<NationalLeaguePlayoffQuarterFinalEvent>();

        Assert.Empty(Fixtures(FixtureType.Playoff));
        Assert.Empty(State.Events);
    }

    private void PlayEliminators(ClubModel? winner56, (int home, int away) score56, ClubModel? winner47, (int home, int away) score47)
    {
        UseNationalLeagueFormat();
        Run<NationalLeaguePlayoffQuarterFinalEvent>();
        var eliminators = Fixtures(FixtureType.Playoff);
        Play(eliminators[0], score56.home, score56.away, winner56);
        Play(eliminators[1], score47.home, score47.away, winner47);
        State.Date = Saturday.AddDays(1);
        State.Notifications.Clear();
    }

    [Fact]
    public void NationalSemiFinals_SecondHostsTheFiveSixWinnerAndThirdHostsTheFourSevenWinner()
    {
        PlayEliminators(Club(6), (1, 2), Club(4), (3, 0));

        Run<NationalLeaguePlayoffSemiFinalEvent>();

        var semis = Fixtures(FixtureType.Playoff).Where(f => f.Round == LastLeagueRound + 2).ToList();
        Assert.Equal([(2, 6), (3, 4)], semis.Select(Ids));
        Assert.All(semis, f => Assert.Equal((Tuesday, new TimeOnly(19, 45)), (f.Date, f.KickOffTime)));
        Assert.Contains("Club 2 v Club 6\nClub 3 v Club 4", Assert.Single(State.Notifications).Message);
        var final = Assert.Single(State.Events.OfType<NationalLeaguePlayoffFinalEvent>());
        Assert.Equal(Tuesday.AddDays(1), final.TriggerDate);
    }

    [Fact]
    public void NationalSemiFinals_UseTheScoreWhenAnEliminatorHasNoWinnerRecorded()
    {
        PlayEliminators(null, (0, 2), null, (2, 1));

        Run<NationalLeaguePlayoffSemiFinalEvent>();

        var semis = Fixtures(FixtureType.Playoff).Where(f => f.Round == LastLeagueRound + 2);
        Assert.Equal([(2, 6), (3, 4)], semis.Select(Ids));
    }

    [Fact]
    public void NationalSemiFinals_WaitUntilBothEliminatorsHaveBeenPlayed()
    {
        PlayEliminators(Club(6), (1, 2), Club(4), (3, 0));
        Fixtures(FixtureType.Playoff)[1].Concluded = false;

        Run<NationalLeaguePlayoffSemiFinalEvent>();

        Assert.Equal(2, Fixtures(FixtureType.Playoff).Count);
        Assert.Empty(State.Notifications);
    }

    [Fact]
    public void NationalFinal_IsBetweenTheSemiFinalWinnersOnTheNextSaturday()
    {
        PlayEliminators(Club(6), (1, 2), Club(4), (3, 0));
        Run<NationalLeaguePlayoffSemiFinalEvent>();
        var semis = Fixtures(FixtureType.Playoff).Where(f => f.Round == LastLeagueRound + 2).ToList();
        Play(semis[0], 0, 1, Club(6));
        Play(semis[1], 2, 2, Club(3));
        State.Date = Tuesday.AddDays(1);
        State.Notifications.Clear();

        Run<NationalLeaguePlayoffFinalEvent>();

        var final = Assert.Single(Fixtures(FixtureType.PlayoffFinal));
        Assert.Equal((6, 3), Ids(final));
        Assert.Equal((new DateOnly(2027, 5, 15), new TimeOnly(15, 0), LastLeagueRound + 3), (final.Date, final.KickOffTime, final.Round));
        Assert.Equal("League 1 Promotion Final", Assert.Single(State.Notifications).Subject);
        Assert.Equal(new DateOnly(2027, 5, 16), Assert.Single(State.Events.OfType<CompetitionEndEvent>()).TriggerDate);
    }

    [Fact]
    public void NationalFinal_WaitsUntilBothSemiFinalsHaveBeenPlayed()
    {
        PlayEliminators(Club(6), (1, 2), Club(4), (3, 0));
        Run<NationalLeaguePlayoffSemiFinalEvent>();
        var semis = Fixtures(FixtureType.Playoff).Where(f => f.Round == LastLeagueRound + 2).ToList();
        Play(semis[0], 0, 1, Club(6));
        State.Date = Tuesday.AddDays(1);
        State.Notifications.Clear();

        Run<NationalLeaguePlayoffFinalEvent>();

        Assert.Empty(Fixtures(FixtureType.PlayoffFinal));
        Assert.Empty(State.Notifications);
    }

    [Fact]
    public void NationalPlayoffs_RunFromEliminatorsToPromotionThroughTheEventQueue()
    {
        UseNationalLeagueFormat();
        _host.Get<IEventManager>().ValidateAndAddEvent(
            ActivatorUtilities.CreateInstance<NationalLeaguePlayoffQuarterFinalEvent>(_host.Services, SeasonEnd, false, _league.Id));

        void PlayDue() =>
            Fixtures(FixtureType.Playoff).Concat(Fixtures(FixtureType.PlayoffFinal))
                .Where(f => !f.Concluded && f.Date <= State.Date)
                .ToList()
                .ForEach(f => Play(f, 0, 1, f.AwayClub));

        for (; State.Date <= new DateOnly(2027, 5, 20); State.Date = State.Date.AddDays(1))
        {
            _host.Get<IEventManager>().ExecuteEvents();
            PlayDue();
        }

        // Away sides win every tie: 6 and 7 through the eliminators, then 6 and 7 again in the semis.
        Assert.Equal((6, 7), Ids(Assert.Single(Fixtures(FixtureType.PlayoffFinal))));
        Assert.Equal([Club(1), Club(7)], _league.PromotedClubs());
        Assert.All(State.Events, e => Assert.True(e.IsCompleted));
    }
}
