using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Competitions;

/// <summary>Germany's second tier, on the real game world from settings.json.</summary>
[Collection(SaveFilesCollection.Name)]
public class GermanyDivisionTwoTests : IDisposable
{
    private const int GermanyDivision1 = 6;
    private const int GermanyDivision2 = 22;

    private readonly GameHost _host = new();

    public void Dispose() => _host.Dispose();

    private static LeagueModel League(IState state, int id) => (LeagueModel)state.Competitions.Single(c => c.Id == id);

    // Draws the relegation play-off and settles it, the second leg going to the given club.
    private void PlayTheRelegationPlayoff(IState state, Func<FixtureModel, ClubModel> winner)
    {
        var draw = state.Events.OfType<RelegationPlayoffEvent>().Single(e => e.CompetitionId == GermanyDivision2);
        state.Date = draw.TriggerDate;
        draw.Execute();

        foreach (var leg in League(state, GermanyDivision2).Fixtures.Where(f => f.FixtureType is FixtureType.Playoff or FixtureType.PlayoffSecondLeg))
            (leg.GoalsHome, leg.GoalsAway, leg.Concluded, leg.ClubWon) = (1, 1, true, leg.FixtureType == FixtureType.PlayoffSecondLeg ? winner(leg) : null);
    }

    // Jumps to the season's end, settling the other countries' play-offs so each division stays the right size.
    private void EndTheSeason(IState state)
    {
        foreach (var league in state.Competitions.OfType<LeagueModel>().Where(l => l.Rank > 1 && l.HasPlayoffs && l.CountryId != 3))
        {
            var winner = league.GenerateLeagueTable().ElementAt(league.AutomaticPromotionPlaces).Club;
            var final = TestData.Result(winner, league.Clubs.First(c => c.Id != winner.Id), 1, 0, FixtureType.PlayoffFinal);
            final.ClubWon = winner;
            league.Fixtures.Add(final);
        }

        state.Date = new DateOnly(2027, 6, 29);
        _host.Get<IProcessHelper>().Process();
    }

    [Fact]
    public void NewGame_BuildsGermanysDivisionsWithEighteenClubsAndARelegationPlayoff()
    {
        var state = _host.StartNewGame(clubId: 9).State;

        var division1 = League(state, GermanyDivision1);
        var division2 = League(state, GermanyDivision2);
        Assert.Equal((3, 2, "Division 2"), (division2.CountryId, division2.Rank, division2.Name));
        Assert.Equal((18, 18), (division1.Clubs.Count, division2.Clubs.Count));
        Assert.Equal(18 * 17, division2.Fixtures.Count);
        Assert.Equal((2, 1), (division1.RelegationPlaces, division1.RelegationPlayoffPlaces));
        Assert.Equal((2, 1, 0), (division2.AutomaticPromotionPlaces, division2.PlayoffPlaces, division2.RelegationPlaces));
        Assert.True(division1.HasPlayoffs);

        var lastMatch = division1.Fixtures.Concat(division2.Fixtures).Max(f => f.Date);
        Assert.Single(state.Events.OfType<RelegationPlayoffEvent>(), e => e.CompetitionId == GermanyDivision2 && e.TriggerDate == lastMatch.AddDays(1));
        Assert.DoesNotContain(state.Events.OfType<EnglishFootballLeaguePlayoffEvent>(), e => e.CompetitionId == GermanyDivision2);
        Assert.DoesNotContain(state.Events.OfType<CompetitionEndEvent>(), e => e.CompetitionId is GermanyDivision1 or GermanyDivision2);
        Assert.All(state.Clubs.Where(c => c.LeagueId == GermanyDivision2), c =>
            Assert.InRange(state.Players.Count(p => p.Contract?.ClubId == c.Id), 25, 30));
    }

    [Fact]
    public void RelegationPlayoff_SixteenthInDivision1PlaysThirdInDivision2()
    {
        var state = _host.StartNewGame(clubId: 9).State;
        var sixteenth = League(state, GermanyDivision1).GenerateLeagueTable().ElementAt(15).Club.Id;
        var third = League(state, GermanyDivision2).GenerateLeagueTable().ElementAt(2).Club.Id;
        var draw = state.Events.OfType<RelegationPlayoffEvent>().Single();

        state.Date = draw.TriggerDate;
        draw.Execute();

        var legs = League(state, GermanyDivision2).Fixtures.Where(f => f.FixtureType is FixtureType.Playoff or FixtureType.PlayoffSecondLeg).ToList();
        Assert.Equal([(sixteenth, third, FixtureType.Playoff), (third, sixteenth, FixtureType.PlayoffSecondLeg)],
            legs.Select(f => (f.HomeClub.Id, f.AwayClub.Id, f.FixtureType)));
        Assert.Contains(state.Events.OfType<CompetitionEndEvent>(), e => e.CompetitionId == GermanyDivision1 && e.TriggerDate == legs[1].Date.AddDays(1));
        Assert.Contains(state.Events.OfType<CompetitionEndEvent>(), e => e.CompetitionId == GermanyDivision2 && e.TriggerDate == legs[1].Date.AddDays(1));
    }

    [Fact]
    public void EndOfSeason_Division2ClubWinningThePlayoff_GoesUpInPlaceOfSixteenth()
    {
        var state = _host.StartNewGame(clubId: 9).State;
        var division1Table = League(state, GermanyDivision1).GenerateLeagueTable().Select(p => p.Club.Id).ToList();
        var division2Table = League(state, GermanyDivision2).GenerateLeagueTable().Select(p => p.Club.Id).ToList();

        PlayTheRelegationPlayoff(state, secondLeg => secondLeg.HomeClub);
        EndTheSeason(state);

        var newDivision1 = League(state, GermanyDivision1);
        var newDivision2 = League(state, GermanyDivision2);
        Assert.Equal((18, 18), (newDivision1.Clubs.Count, newDivision2.Clubs.Count));
        Assert.All(division2Table.Take(3), id => Assert.Contains(newDivision1.Clubs, c => c.Id == id));
        Assert.All(division1Table.TakeLast(3), id => Assert.Contains(newDivision2.Clubs, c => c.Id == id));
    }

    [Fact]
    public void EndOfSeason_SixteenthWinningThePlayoff_StaysUp()
    {
        var state = _host.StartNewGame(clubId: 9).State;
        var division1Table = League(state, GermanyDivision1).GenerateLeagueTable().Select(p => p.Club.Id).ToList();
        var division2Table = League(state, GermanyDivision2).GenerateLeagueTable().Select(p => p.Club.Id).ToList();

        PlayTheRelegationPlayoff(state, secondLeg => secondLeg.AwayClub);
        EndTheSeason(state);

        var newDivision1 = League(state, GermanyDivision1);
        var newDivision2 = League(state, GermanyDivision2);
        Assert.Equal((18, 18), (newDivision1.Clubs.Count, newDivision2.Clubs.Count));
        Assert.Contains(newDivision1.Clubs, c => c.Id == division1Table[15]);
        Assert.Contains(newDivision2.Clubs, c => c.Id == division2Table[2]);
        Assert.All(division2Table.Take(2), id => Assert.Contains(newDivision1.Clubs, c => c.Id == id));
        Assert.All(division1Table.TakeLast(2), id => Assert.Contains(newDivision2.Clubs, c => c.Id == id));
    }
}
