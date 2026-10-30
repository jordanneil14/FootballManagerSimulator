using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Competitions;

/// <summary>Italy's second tier and its play-offs, on the real game world from settings.json.</summary>
[Collection(SaveFilesCollection.Name)]
public class ItalyDivisionTwoTests : IDisposable
{
    private const int ItalyDivision1 = 7;
    private const int ItalyDivision2 = 23;

    private readonly GameHost _host = new();

    public void Dispose() => _host.Dispose();

    private static LeagueModel League(IState state, int id) => (LeagueModel)state.Competitions.Single(c => c.Id == id);

    private static List<int> Table(IState state, int id) => League(state, id).GenerateLeagueTable().Select(p => p.Club.Id).ToList();

    private static List<FixtureModel> Playoffs(IState state, FixtureType type) =>
        League(state, ItalyDivision2).Fixtures.Where(f => f.FixtureType == type).ToList();

    // Runs the next round's draw, the event the previous round left behind.
    private static void DrawNextRound(IState state)
    {
        var draw = state.Events.OfType<SerieBPlayoffEvent>().Where(e => !e.IsCompleted).MinBy(e => e.TriggerDate)!;
        state.Date = draw.TriggerDate;
        draw.Execute();
        draw.IsCompleted = true;
    }

    private static void Play(FixtureModel fixture, int home, int away, ClubModel? winner) =>
        (fixture.GoalsHome, fixture.GoalsAway, fixture.Concluded, fixture.ClubWon) = (home, away, true, winner);

    private static (int, int) Ids(FixtureModel f) => (f.HomeClub.Id, f.AwayClub.Id);

    [Fact]
    public void NewGame_BuildsItalysDivisionsAndTheSerieBPlayoffs()
    {
        var state = _host.StartNewGame(clubId: 9).State;

        var division1 = League(state, ItalyDivision1);
        var division2 = League(state, ItalyDivision2);
        Assert.Equal((4, 2, "Division 2"), (division2.CountryId, division2.Rank, division2.Name));
        Assert.Equal((20, 20), (division1.Clubs.Count, division2.Clubs.Count));
        Assert.Equal(20 * 19, division2.Fixtures.Count);
        Assert.Equal((2, 6, 0, PlayoffFormat.SerieB), (division2.AutomaticPromotionPlaces, division2.PlayoffPlaces, division2.RelegationPlaces, division2.PlayoffFormat));
        Assert.Single(state.Events.OfType<SerieBPlayoffEvent>(), e => e.CompetitionId == ItalyDivision2 && e.TriggerDate == division2.Fixtures.Max(f => f.Date).AddDays(1));
        Assert.DoesNotContain(state.Events.OfType<EnglishFootballLeaguePlayoffEvent>(), e => e.CompetitionId == ItalyDivision2);
        Assert.DoesNotContain(state.Events.OfType<CompetitionEndEvent>(), e => e.CompetitionId == ItalyDivision2);
        Assert.All(state.Clubs.Where(c => c.LeagueId == ItalyDivision2), c =>
            Assert.InRange(state.Players.Count(p => p.Contract?.ClubId == c.Id), 25, 30));
    }

    [Fact]
    public void Playoffs_RunThroughPreliminariesTwoLeggedSemiFinalsAndATwoLeggedFinal()
    {
        var state = _host.StartNewGame(clubId: 9).State;
        var table = Table(state, ItalyDivision2);
        ClubModel Club(int position) => League(state, ItalyDivision2).Clubs.Single(c => c.Id == table[position - 1]);

        DrawNextRound(state);
        var preliminaries = Playoffs(state, FixtureType.Playoff);
        Assert.Equal([(table[4], table[7]), (table[5], table[6])], preliminaries.Select(Ids));

        // 5th v 8th is level, so 5th go through; 7th win away at 6th.
        Play(preliminaries[0], 1, 1, null);
        Play(preliminaries[1], 0, 1, Club(7));
        DrawNextRound(state);

        var semiFirstLegs = Playoffs(state, FixtureType.Playoff).Except(preliminaries).ToList();
        var semiSecondLegs = Playoffs(state, FixtureType.PlayoffSecondLeg);
        Assert.Equal([(table[6], table[2]), (table[4], table[3])], semiFirstLegs.Select(Ids));
        Assert.Equal([(table[2], table[6]), (table[3], table[4])], semiSecondLegs.Select(Ids));
        Assert.All(semiFirstLegs, f => Assert.Equal(DayOfWeek.Tuesday, f.Date.DayOfWeek));
        Assert.All(semiSecondLegs, f => Assert.Equal(semiFirstLegs[0].Date.AddDays(4), f.Date));

        // 7th knock out 3rd; 4th go through on a level aggregate as the higher-placed club.
        Play(semiFirstLegs[0], 2, 0, Club(7));
        Play(semiSecondLegs[0], 1, 0, Club(7));
        Play(semiFirstLegs[1], 1, 1, null);
        Play(semiSecondLegs[1], 0, 0, Club(4));
        DrawNextRound(state);

        var finalSecondLeg = Assert.Single(Playoffs(state, FixtureType.PlayoffSecondLeg).Except(semiSecondLegs));
        Assert.Equal((table[3], table[6]), Ids(finalSecondLeg));
        Assert.Contains(Playoffs(state, FixtureType.Playoff), f => Ids(f) == (table[6], table[3]) && f.Round == finalSecondLeg.Round);
        Assert.True(League(state, ItalyDivision2).IsTwoLeggedPlayoffFinal(finalSecondLeg));
        Assert.False(League(state, ItalyDivision2).IsTwoLeggedPlayoffFinal(semiSecondLegs[0]));
        Assert.Contains(state.Events.OfType<CompetitionEndEvent>(), e => e.CompetitionId == ItalyDivision2 && e.TriggerDate == finalSecondLeg.Date.AddDays(1));

        Play(finalSecondLeg, 2, 1, Club(7));
        Assert.Equal([table[0], table[1], table[6]], League(state, ItalyDivision2).PromotedClubs().Select(c => c.Id));
    }

    [Fact]
    public void MatchEngine_ALevelDecidingTieGoesToTheHigherPlacedClubWithoutExtraTime()
    {
        var state = _host.StartNewGame(clubId: 9).State;
        var division2 = League(state, ItalyDivision2);
        var table = Table(state, ItalyDivision2);
        var simulator = _host.Get<IMatchSimulatorHelper>();

        DrawNextRound(state);
        var preliminaries = Playoffs(state, FixtureType.Playoff);
        Play(preliminaries[0], 0, 0, null);
        preliminaries[0].Concluded = false;
        simulator.FinaliseMatch(preliminaries[0], division2);
        Assert.Equal(table[4], preliminaries[0].ClubWon!.Id);

        Play(preliminaries[1], 2, 0, preliminaries[1].HomeClub);
        DrawNextRound(state);

        // Level on the night of a first leg is just a draw.
        var firstLeg = Playoffs(state, FixtureType.Playoff).Except(preliminaries).First();
        (firstLeg.GoalsHome, firstLeg.GoalsAway) = (1, 1);
        simulator.FinaliseMatch(firstLeg, division2);
        Assert.Null(firstLeg.ClubWon);

        var secondLeg = Playoffs(state, FixtureType.PlayoffSecondLeg).First();
        (secondLeg.GoalsHome, secondLeg.GoalsAway) = (0, 0);
        simulator.FinaliseMatch(secondLeg, division2);
        Assert.Equal(table[2], secondLeg.ClubWon!.Id);
    }

    [Fact]
    public void EndOfSeason_SwapsThreeClubsBetweenItalysDivisions()
    {
        var state = _host.StartNewGame(clubId: 9).State;
        var relegated = Table(state, ItalyDivision1).TakeLast(3).ToList();
        var division2Table = Table(state, ItalyDivision2);
        var playoffWinner = division2Table[7]; // eighth, to show it's the play-off winner that goes up
        var promoted = new[] { division2Table[0], division2Table[1], playoffWinner };

        TestData.SettlePlayoffs(state, league => league.Id == ItalyDivision2
            ? league.Clubs.Single(c => c.Id == playoffWinner)
            : league.GenerateLeagueTable().ElementAt(league.AutomaticPromotionPlaces).Club);
        state.Date = new DateOnly(2027, 6, 29);
        _host.Get<IProcessHelper>().Process();

        var newDivision1 = League(state, ItalyDivision1);
        var newDivision2 = League(state, ItalyDivision2);
        Assert.Equal((20, 20), (newDivision1.Clubs.Count, newDivision2.Clubs.Count));
        Assert.All(promoted, id => Assert.Contains(newDivision1.Clubs, c => c.Id == id));
        Assert.All(relegated, id => Assert.Contains(newDivision2.Clubs, c => c.Id == id));
        Assert.Single(state.Events.OfType<SerieBPlayoffEvent>(), e => e.CompetitionId == ItalyDivision2);
    }
}
