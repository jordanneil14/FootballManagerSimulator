using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Competitions;

/// <summary>Spain's second tier, on the real game world from settings.json.</summary>
[Collection(SaveFilesCollection.Name)]
public class SpainDivisionTwoTests : IDisposable
{
    private const int SpainDivision1 = 5;
    private const int SpainDivision2 = 21;

    private readonly GameHost _host = new();

    public void Dispose() => _host.Dispose();

    private static LeagueModel League(IState state, int id) => (LeagueModel)state.Competitions.Single(c => c.Id == id);

    private static double AverageRating(IState state, int leagueId)
    {
        var clubIds = state.Clubs.Where(c => c.LeagueId == leagueId).Select(c => c.Id).ToHashSet();
        return state.Players.Where(p => p.Contract != null && clubIds.Contains(p.Contract.ClubId)).Average(p => p.Rating);
    }

    // Jumps to the season's end, settling every play-off on the way so each division stays the right size.
    private void EndTheSeason(GameEngine engine, Func<LeagueModel, ClubModel>? playoffWinner = null)
    {
        TestData.SettlePlayoffs(engine.State, playoffWinner);

        engine.State.Date = new DateOnly(2027, 6, 29);
        _host.Get<IProcessHelper>().Process();
    }

    [Fact]
    public void NewGame_BuildsSpainDivision2WithTwentyTwoClubsAndItsPlayoffs()
    {
        var state = _host.StartNewGame(clubId: 9).State;

        var division2 = League(state, SpainDivision2);
        Assert.Equal((2, 2, "Division 2"), (division2.CountryId, division2.Rank, division2.Name));
        Assert.Equal(22, division2.Clubs.Count);
        Assert.Equal(22 * 21, division2.Fixtures.Count);
        Assert.Equal((2, 4, 0), (division2.AutomaticPromotionPlaces, division2.PlayoffPlaces, division2.RelegationPlaces));
        Assert.True(division2.HasPlayoffs);
        Assert.Single(state.Events.OfType<EnglishFootballLeaguePlayoffEvent>(), e => e.CompetitionId == SpainDivision2);
        Assert.DoesNotContain(state.Events.OfType<CompetitionEndEvent>(), e => e.CompetitionId == SpainDivision2);
    }

    [Fact]
    public void NewGame_GivesSpainDivision2SquadsAndBudgetsForASecondTier()
    {
        var state = _host.StartNewGame(clubId: 9).State;

        Assert.True(AverageRating(state, SpainDivision2) < AverageRating(state, SpainDivision1));
        Assert.InRange(AverageRating(state, SpainDivision2), AverageRating(state, 2) - 3, AverageRating(state, 2) + 3);
        Assert.All(state.Clubs.Where(c => c.LeagueId == SpainDivision2), c =>
        {
            Assert.InRange(state.Players.Count(p => p.Contract?.ClubId == c.Id), 25, 30);
            Assert.True(c.TransferBudget > 0);
        });
        var averageBudget = (int Id) => state.Clubs.Where(c => c.LeagueId == Id).Average(c => c.TransferBudget);
        Assert.True(averageBudget(SpainDivision2) < averageBudget(SpainDivision1));
    }

    [Fact]
    public void Playoffs_ThirdToSixthPlayForTheLastPromotionPlace()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var state = engine.State;
        var division2 = League(state, SpainDivision2);
        var table = division2.GenerateLeagueTable().Select(p => p.Club.Id).ToList();
        var semis = state.Events.OfType<EnglishFootballLeaguePlayoffEvent>().Single(e => e.CompetitionId == SpainDivision2);

        state.Date = semis.TriggerDate;
        semis.Execute();

        var firstLegs = division2.Fixtures.Where(f => f.FixtureType == FixtureType.Playoff).ToList();
        Assert.Equal([(table[2], table[5]), (table[3], table[4])], firstLegs.Select(f => (f.HomeClub.Id, f.AwayClub.Id)));

        foreach (var tie in division2.Fixtures.Where(f => f.FixtureType is FixtureType.Playoff or FixtureType.PlayoffSecondLeg))
            (tie.GoalsHome, tie.GoalsAway, tie.Concluded, tie.ClubWon) = (1, 0, true, tie.FixtureType == FixtureType.PlayoffSecondLeg ? tie.HomeClub : null);
        var finalDraw = state.Events.OfType<EnglishFootballLeaguePlayoffFinalEvent>().Single(e => e.CompetitionId == SpainDivision2);
        state.Date = finalDraw.TriggerDate;
        finalDraw.Execute();

        var final = Assert.Single(division2.Fixtures, f => f.FixtureType == FixtureType.PlayoffFinal);
        Assert.Equal([table[5], table[4]], new[] { final.HomeClub.Id, final.AwayClub.Id });
        Assert.Contains(state.Events.OfType<CompetitionEndEvent>(), e => e.CompetitionId == SpainDivision2 && e.TriggerDate == final.Date.AddDays(1));
    }

    [Fact]
    public void EndOfSeason_SwapsThreeClubsBetweenSpainsDivisions()
    {
        var engine = _host.StartNewGame(clubId: 9);
        var state = engine.State;
        var relegated = League(state, SpainDivision1).GenerateLeagueTable().TakeLast(3).Select(p => p.Club.Id).ToList();
        var division2Table = League(state, SpainDivision2).GenerateLeagueTable().Select(p => p.Club).ToList();
        var playoffWinner = division2Table[4]; // fifth, to show it's the play-off winner and not third that goes up
        var promoted = new[] { division2Table[0].Id, division2Table[1].Id, playoffWinner.Id };

        EndTheSeason(engine, league => league.Id == SpainDivision2 ? playoffWinner : league.GenerateLeagueTable().ElementAt(league.AutomaticPromotionPlaces).Club);

        var newDivision1 = League(state, SpainDivision1);
        var newDivision2 = League(state, SpainDivision2);
        Assert.Equal((20, 22), (newDivision1.Clubs.Count, newDivision2.Clubs.Count));
        Assert.All(promoted, id => Assert.Contains(newDivision1.Clubs, c => c.Id == id));
        Assert.All(relegated, id => Assert.Contains(newDivision2.Clubs, c => c.Id == id));
        Assert.DoesNotContain(newDivision1.Clubs, c => c.Id == division2Table[2].Id);
        Assert.All(newDivision1.Clubs.Concat(newDivision2.Clubs), c => Assert.Equal(c.LeagueId, newDivision1.Clubs.Contains(c) ? SpainDivision1 : SpainDivision2));
        Assert.Single(state.Events.OfType<EnglishFootballLeaguePlayoffEvent>(), e => e.CompetitionId == SpainDivision2);
    }
}
