using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Maui.Events;

// Draws each round of a Serie B-format play-off, working out the round from the play-off fixtures
// already played: one-off preliminaries (5th v 8th, 6th v 7th, the higher-placed club at home),
// two-legged semi-finals (3rd v the 6th/7th winner, 4th v the 5th/8th winner) and a two-legged final.
// In both two-legged rounds the higher-placed club is at home in the second leg.
public class SerieBPlayoffEvent(
    IState state,
    INotificationFactory notificationFactory,
    IEventManager eventManager,
    IServiceProvider serviceProvider,
    DateOnly triggerDate,
    bool isCompleted,
    int competitionId) : Event(triggerDate, isCompleted)
{
    private const int Places = 6;

    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    private readonly IEventManager EventManager = eventManager;
    private readonly IServiceProvider ServiceProvider = serviceProvider;
    public int CompetitionId { get; } = competitionId;

    public override void Execute()
    {
        var league = (LeagueModel)State.Competitions.First(p => p.Id == CompetitionId);

        var playoffs = league.Fixtures
            .Where(p => p.FixtureType is FixtureType.Playoff or FixtureType.PlayoffSecondLeg)
            .ToList();

        if (playoffs.Count == 0)
            DrawPreliminaries(league);
        else if (!playoffs.Any(p => p.FixtureType == FixtureType.PlayoffSecondLeg))
            DrawSemiFinals(league, playoffs);
        else
            DrawFinal(league, playoffs);
    }

    private void DrawPreliminaries(LeagueModel league)
    {
        var clubs = league.GenerateLeagueTable()
            .Skip(league.AutomaticPromotionPlaces)
            .Take(Places)
            .Select(p => p.Club)
            .ToList();

        if (clubs.Count < Places)
            return;

        var date = Next(DayOfWeek.Saturday);
        var round = NextRound(league);
        var ties = new[] { (clubs[2], clubs[5]), (clubs[3], clubs[4]) };
        foreach (var (home, away) in ties)
            league.Fixtures.Add(Fixture(home, away, round, date, new TimeOnly(15, 00), FixtureType.Playoff));

        Notify(league, "Playoffs",
            $"The {league.Name} playoff preliminaries have been drawn:\n" +
            string.Join("\n", ties.Select(t => $"{t.Item1.Name} v {t.Item2.Name}")) +
            $"\nFixtures will be played on {date:d MMMM yyyy}");

        ScheduleNextRound(date);
    }

    private void DrawSemiFinals(LeagueModel league, List<FixtureModel> preliminaries)
    {
        if (preliminaries.Count != 2 || preliminaries.Any(p => !p.Concluded))
            return;

        var table = league.GenerateLeagueTable().Skip(league.AutomaticPromotionPlaces).Select(p => p.Club).ToList();
        var sixthVSeventh = preliminaries.First(p => p.HomeClub.Id == table[3].Id);
        var fifthVEighth = preliminaries.First(p => p != sixthVSeventh);

        var ties = DrawTwoLeggedRound(league, [(table[0], Winner(sixthVSeventh)), (table[1], Winner(fifthVEighth))]);

        Notify(league, "Playoffs",
            $"The {league.Name} playoff semi-finals have been drawn:\n" +
            string.Join("\n", ties.Select(t => $"{t.FirstLeg.HomeClub.Name} v {t.FirstLeg.AwayClub.Name}")) +
            $"\n1st Leg: {ties[0].FirstLeg.Date:d MMMM yyyy}\n2nd Leg: {ties[0].SecondLeg.Date:d MMMM yyyy}");

        ScheduleNextRound(ties[0].SecondLeg.Date);
    }

    private void DrawFinal(LeagueModel league, List<FixtureModel> playoffs)
    {
        var semiFinalRound = playoffs.Where(p => p.FixtureType == FixtureType.PlayoffSecondLeg).Max(p => p.Round);
        var secondLegs = playoffs.Where(p => p.FixtureType == FixtureType.PlayoffSecondLeg && p.Round == semiFinalRound).ToList();
        if (secondLegs.Count != 2 || secondLegs.Any(p => !p.Concluded || p.ClubWon == null))
            return;

        var (higher, lower) = Ordered(league, secondLegs[0].ClubWon!, secondLegs[1].ClubWon!);
        var final = DrawTwoLeggedRound(league, [(higher, lower)])[0];

        CompetitionEndEvent.Schedule(EventManager, ServiceProvider, final.SecondLeg.Date, CompetitionId);

        Notify(league, "Playoff Final",
            $"The {league.Name} playoff final has been set:\n" +
            $"{final.FirstLeg.HomeClub.Name} v {final.FirstLeg.AwayClub.Name}\n" +
            $"1st Leg: {final.FirstLeg.Date:d MMMM yyyy}\n2nd Leg: {final.SecondLeg.Date:d MMMM yyyy}");
    }

    // The lower-placed club hosts the first leg on Tuesday, the higher-placed the second on Saturday.
    private List<(FixtureModel FirstLeg, FixtureModel SecondLeg)> DrawTwoLeggedRound(LeagueModel league, List<(ClubModel Higher, ClubModel Lower)> ties)
    {
        var firstLegDate = Next(DayOfWeek.Tuesday);
        var secondLegDate = firstLegDate.AddDays(4);
        var round = NextRound(league);

        var legs = ties
            .Select(t => (
                FirstLeg: Fixture(t.Lower, t.Higher, round, firstLegDate, new TimeOnly(19, 45), FixtureType.Playoff),
                SecondLeg: Fixture(t.Higher, t.Lower, round, secondLegDate, new TimeOnly(15, 00), FixtureType.PlayoffSecondLeg)))
            .ToList();

        foreach (var (firstLeg, secondLeg) in legs)
        {
            league.Fixtures.Add(firstLeg);
            league.Fixtures.Add(secondLeg);
        }

        return legs;
    }

    // A level preliminary goes to the home side, which finished higher.
    private static ClubModel Winner(FixtureModel fixture) => fixture.ClubWon ?? fixture.HomeClub;

    private static (ClubModel Higher, ClubModel Lower) Ordered(LeagueModel league, ClubModel a, ClubModel b) =>
        league.HigherPlacedClub(a, b).Id == a.Id ? (a, b) : (b, a);

    private static FixtureModel Fixture(ClubModel home, ClubModel away, int round, DateOnly date, TimeOnly kickOff, FixtureType type) => new()
    {
        HomeClub = home,
        AwayClub = away,
        Round = round,
        Date = date,
        KickOffTime = kickOff,
        FixtureType = type
    };

    private static int NextRound(LeagueModel league) => league.Fixtures.Max(p => p.Round) + 1;

    private DateOnly Next(DayOfWeek day)
    {
        var date = State.Date;
        while (date.DayOfWeek != day)
            date = date.AddDays(1);
        return date;
    }

    private void ScheduleNextRound(DateOnly lastMatchDate)
    {
        var nextRound = ActivatorUtilities.CreateInstance<SerieBPlayoffEvent>(
            ServiceProvider, lastMatchDate.AddDays(1), false, CompetitionId);
        EventManager.ValidateAndAddEvent(nextRound);
    }

    private void Notify(LeagueModel league, string subject, string message) =>
        NotificationFactory.AddNotification(State.Date, "League", $"{league.Name} {subject}", message);

    public override (bool success, string errorMessage) ValidateAdd()
    {
        return (true, "");
    }
}
