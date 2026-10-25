using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Tests.Events;

public class CompetitionEndEventTests : IDisposable
{
    private readonly GameHost _host = new(new SettingsModel { Countries = [new SettingsModel.IdNameModel { Id = 1, Name = "England" }] });
    private readonly ClubModel[] _clubs = Enumerable.Range(1, 4).Select(i => TestData.Club(i)).ToArray();
    private readonly LeagueModel _top;
    private readonly LeagueModel _second;

    public CompetitionEndEventTests()
    {
        _host.State.Date = new DateOnly(2027, 5, 9);
        _host.State.MyClubId = 3;
        _host.State.Clubs.AddRange(_clubs);

        // Club 1 beats club 2 at the top; club 3 beats club 4 in the league below.
        _top = TestData.League(1, [_clubs[0], _clubs[1]]);
        _top.RelegationPlaces = 1;
        _top.Fixtures.Add(TestData.Result(_clubs[0], _clubs[1], 2, 0));

        _second = TestData.League(2, [_clubs[2], _clubs[3]]);
        _second.Rank = 2;
        _second.AutomaticPromotionPlaces = 1;
        _second.Fixtures.Add(TestData.Result(_clubs[2], _clubs[3], 1, 0));

        _host.State.Competitions.AddRange([_top, _second]);
    }

    public void Dispose() => _host.Dispose();

    private void Run(int competitionId) =>
        ActivatorUtilities.CreateInstance<CompetitionEndEvent>(_host.Services, _host.State.Date, false, competitionId).Execute();

    [Fact]
    public void League_AnnouncesItsRelegatedClubs()
    {
        _host.State.MyClubId = 1;

        Run(_top.Id);

        var notification = Assert.Single(_host.State.Notifications);
        Assert.Equal("England - League 1 Promotion & Relegation", notification.Subject);
        Assert.Contains("Relegated:\n\tClub 2", notification.Message);
        Assert.DoesNotContain("Promoted", notification.Message);
        Assert.Equal(BoardConfidenceRules.Starting, _host.State.BoardConfidence);
    }

    [Fact]
    public void League_WhenMyClubIsPromoted_TheChairmanCongratulatesMeAndConfidenceRises()
    {
        Run(_second.Id);

        Assert.Contains(_host.State.Notifications, n => n.Message.Contains("Promoted:\n\tClub 3"));
        var chairman = Assert.Single(_host.State.Notifications, n => n.Recipient == "Chairman");
        Assert.Equal("Promotion", chairman.Subject);
        Assert.Equal(BoardConfidenceRules.Starting + BoardConfidenceRules.Promotion, _host.State.BoardConfidence);
    }

    [Fact]
    public void League_IncludesThePlayoffWinnerAmongThePromoted()
    {
        _second.Fixtures.Add(TestData.Result(_clubs[3], _clubs[2], 1, 0, FixtureType.PlayoffFinal));
        _second.Fixtures.Last().ClubWon = _clubs[3];

        Run(_second.Id);

        Assert.Contains(_host.State.Notifications, n => n.Message.Contains("Promoted:\n\tClub 3\n\tClub 4"));
    }

    [Fact]
    public void Cup_AnnouncesTheWinner()
    {
        var cup = TestData.LeagueCup(new DateOnly(2027, 5, 1));
        var final = TestData.Result(_clubs[0], _clubs[2], 0, 1);
        final.Round = 1;
        final.ClubWon = _clubs[2];
        cup.Fixtures.Add(final);
        _host.State.Competitions.Add(cup);

        Run(cup.Id);

        var notification = Assert.Single(_host.State.Notifications);
        Assert.Equal("Cup Winners", notification.Subject);
        Assert.Equal("Club 3 have won the Cup, beating Club 1 in the final.", notification.Message);
    }

    [Fact]
    public void ScheduleForCompetitions_RunsTheDayAfterEachFinalMatch_LeavingPlayoffLeaguesToTheirFinal()
    {
        _top.Fixtures[0].Date = new DateOnly(2027, 5, 8);
        _second.Type = CompetitionType.EnglishFootballLeague;
        _second.PlayoffPlaces = 4;
        var cup = TestData.LeagueCup(new DateOnly(2027, 3, 1), new DateOnly(2027, 5, 15));
        _host.State.Competitions.AddRange([cup, new FriendlyModel { Id = 9 }]);

        CompetitionEndEvent.ScheduleForCompetitions(_host.State, _host.Get<IEventManager>(), _host.Services);

        var events = _host.State.Events.OfType<CompetitionEndEvent>().ToDictionary(e => e.CompetitionId, e => e.TriggerDate);
        Assert.Equal(new DateOnly(2027, 5, 9), events[_top.Id]);
        Assert.Equal(new DateOnly(2027, 5, 16), events[cup.Id]);
        Assert.Equal(2, events.Count);
    }
}
