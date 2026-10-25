using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Tests.Competitions;

public class ScottishPremiershipTests : IDisposable
{
    private readonly GameHost _host = new(new SettingsModel());
    private readonly LeagueModel _league;
    private readonly ScottishPremiershipService _service;

    public ScottishPremiershipTests()
    {
        _host.State.Date = new DateOnly(2026, 7, 1);
        _host.State.MyClubId = 1;
        _host.State.Clubs.AddRange(Enumerable.Range(1, 12).Select(i => TestData.Club(i)));

        _league = TestData.League(16, _host.State.Clubs, CompetitionType.ScottishPremiership);
        _league.PlayoffPlaces = 6;
        _league.RelegationPlaces = 6;
        _host.State.Competitions.Add(_league);

        _service = _host.Services.GetRequiredService<ScottishPremiershipService>();
    }

    public void Dispose() => _host.Dispose();

    private static int Meetings(IEnumerable<FixtureModel> fixtures, int home, int away) =>
        fixtures.Count(f => f.HomeClub.Id == home && f.AwayClub.Id == away);

    // Club n wins every home game against clubs below it, so the table runs 1 to 12 by points.
    private void PlayFirstPhase(DateOnly splitDate)
    {
        foreach (var fixture in _league.Fixtures)
        {
            var homeIsBetter = fixture.HomeClub.Id < fixture.AwayClub.Id;
            fixture.GoalsHome = homeIsBetter ? 1 : 0;
            fixture.GoalsAway = homeIsBetter ? 0 : 1;
            fixture.Concluded = true;
        }

        _host.State.Date = splitDate;
    }

    [Fact]
    public void EveryClubPlaysEveryOtherThreeTimes_TwiceAtOneGroundAndOnceAtTheOther()
    {
        _service.GenerateNextRoundOfFixtures(_league);

        Assert.Equal(12 * 11 / 2 * 3, _league.Fixtures.Count);
        foreach (var a in _league.Clubs)
        foreach (var b in _league.Clubs.Where(c => c.Id > a.Id))
        {
            var meetings = new[] { Meetings(_league.Fixtures, a.Id, b.Id), Meetings(_league.Fixtures, b.Id, a.Id) };
            Assert.Equal(new[] { 1, 2 }, meetings.OrderBy(m => m));
        }

        Assert.All(_league.Clubs, c => Assert.InRange(_league.Fixtures.Count(f => f.HomeClub.Id == c.Id), 16, 17));
    }

    [Fact]
    public void ThirtyThreeMatchdaysOnSaturdaysBetweenAugustAndMarch()
    {
        _service.GenerateNextRoundOfFixtures(_league);

        var rounds = _league.Fixtures.GroupBy(f => f.Round).ToList();
        Assert.Equal(Enumerable.Range(1, 33), rounds.Select(r => r.Key).OrderBy(r => r));
        foreach (var round in rounds)
        {
            Assert.Single(round.Select(f => f.Date).Distinct());
            Assert.Equal(12, round.SelectMany(f => new[] { f.HomeClub.Id, f.AwayClub.Id }).Distinct().Count());
        }

        Assert.Equal(33, _league.Fixtures.Select(f => f.Date).Distinct().Count());
        Assert.All(_league.Fixtures, f =>
        {
            Assert.Equal(DayOfWeek.Saturday, f.Date.DayOfWeek);
            Assert.InRange(f.Date, new DateOnly(2026, 8, 1), new DateOnly(2027, 3, 31));
            Assert.Equal(FixtureType.Base, f.FixtureType);
        });
    }

    [Fact]
    public void Split_EachHalfPlaysItselfOnceMore_LevellingEveryPairingAtTwoHomeGamesEach()
    {
        _service.GenerateNextRoundOfFixtures(_league);
        PlayFirstPhase(new DateOnly(2027, 3, 28));
        var firstPhase = _league.Fixtures.ToList();

        var lastMatch = _service.GenerateSplitFixtures(_league);

        Assert.Equal(Enumerable.Range(1, 6), _league.TopSplitClubIds.OrderBy(i => i));
        var split = _league.Fixtures.Except(firstPhase).ToList();
        Assert.Equal(2 * (6 * 5 / 2), split.Count);
        Assert.All(split, f => Assert.Equal(f.HomeClub.Id <= 6, f.AwayClub.Id <= 6));

        foreach (var a in _league.Clubs)
        foreach (var b in _league.Clubs.Where(c => c.Id > a.Id && (c.Id <= 6) == (a.Id <= 6)))
        {
            Assert.Equal(2, Meetings(_league.Fixtures, a.Id, b.Id));
            Assert.Equal(2, Meetings(_league.Fixtures, b.Id, a.Id));
        }

        // Five consecutive Saturdays starting the first Saturday after the split, rounds 34 to 38.
        var dates = split.Select(f => f.Date).Distinct().OrderBy(d => d).ToList();
        Assert.Equal(Enumerable.Range(0, 5).Select(i => new DateOnly(2027, 4, 3).AddDays(7 * i)), dates);
        Assert.Equal(dates.Last(), lastMatch);
        Assert.Equal(Enumerable.Range(34, 5), split.Select(f => f.Round).Distinct().OrderBy(r => r));
        Assert.All(split.GroupBy(f => f.Round), round => Assert.Single(round.Select(f => f.Date).Distinct()));
    }

    [Fact]
    public void AfterTheSplit_TheTopHalfStaysAboveTheBottomHalfWhateverThePoints()
    {
        _service.GenerateNextRoundOfFixtures(_league);
        PlayFirstPhase(new DateOnly(2027, 3, 28));
        _service.GenerateSplitFixtures(_league);

        // Seventh place wins all of its split games and every other split game is drawn,
        // taking seventh (45 + 15 points) past sixth (54 + 5).
        foreach (var fixture in _league.Fixtures.Where(f => !f.Concluded))
        {
            fixture.GoalsHome = fixture.HomeClub.Id == 7 ? 1 : 0;
            fixture.GoalsAway = fixture.AwayClub.Id == 7 ? 1 : 0;
            fixture.Concluded = true;
        }

        var table = _league.GenerateLeagueTable().ToList();
        Assert.True(table.Single(p => p.Club.Id == 7).Points > table.Single(p => p.Club.Id == 6).Points);
        Assert.Equal(Enumerable.Range(1, 6), table.Take(6).Select(p => p.Club.Id).OrderBy(i => i));
        Assert.Equal(7, table[6].Club.Id);
    }

    [Fact]
    public void SplitEvent_AnnouncesTheHalvesAndSchedulesTheEndOfSeason_OnlyOnce()
    {
        _service.GenerateNextRoundOfFixtures(_league);
        PlayFirstPhase(new DateOnly(2027, 3, 28));
        var split = ActivatorUtilities.CreateInstance<ScottishPremiershipSplitEvent>(_host.Services, _host.State.Date, false, _league.Id);

        split.Execute();
        split.Execute();

        Assert.Equal(198 + 30, _league.Fixtures.Count);
        var notification = Assert.Single(_host.State.Notifications);
        Assert.Equal("League 16 Split", notification.Subject);
        Assert.Contains("Top half:\n\tClub 1\n\tClub 2\n\tClub 3\n\tClub 4\n\tClub 5\n\tClub 6\n", notification.Message);
        var end = Assert.Single(_host.State.Events.OfType<CompetitionEndEvent>());
        Assert.Equal(new DateOnly(2027, 5, 2), end.TriggerDate);
    }

    [Fact]
    public void Provider_SchedulesTheSplitForTheDayAfterTheThirtyThirdMatchday()
    {
        var provider = _host.Services.GetServices<ICompetitionProvider>().Single(p => p.Type == CompetitionType.ScottishPremiership);
        _host.State.Clubs.ForEach(c => c.LeagueId = 16);

        var league = provider.CreateCompetition(new CompetitionModel
        {
            Id = 16,
            Name = "Division 1",
            Rank = 1,
            CountryId = 7,
            LeagueTable = new CompetitionModel.LeagueTableModel { Places = 12, PlayoffPlaces = 6, RelegationPlaces = 6 }
        });

        Assert.True(((LeagueModel)league).HasPlayoffs);
        var split = Assert.Single(_host.State.Events.OfType<ScottishPremiershipSplitEvent>());
        Assert.Equal(league.Fixtures.Max(f => f.Date).AddDays(1), split.TriggerDate);
    }
}
