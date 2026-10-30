using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Maui.Events;

// A two-legged tie between the club just above the league's automatic relegation places and the club
// just below the automatic promotion places in the league beneath it (e.g. Germany's 16th v 3rd).
// The fixtures belong to the lower league, which owns the play-off place, and the winner plays in the
// league above next season.
public class RelegationPlayoffEvent(
    IState state,
    INotificationFactory notificationFactory,
    IEventManager eventManager,
    IServiceProvider serviceProvider,
    DateOnly triggerDate,
    bool isCompleted,
    int competitionId) : Event(triggerDate, isCompleted)
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    private readonly IEventManager EventManager = eventManager;
    private readonly IServiceProvider ServiceProvider = serviceProvider;
    public int CompetitionId { get; } = competitionId;

    public override void Execute()
    {
        var lower = (LeagueModel)State.Competitions.First(p => p.Id == CompetitionId);
        if (LeagueAbove(State, lower) is not { } upper)
            return;

        var upperTable = upper.GenerateLeagueTable().ToList();
        var lowerTable = lower.GenerateLeagueTable().ToList();

        var upperPosition = upperTable.Count - upper.RelegationPlaces - 1;
        if (upperPosition < 0 || lowerTable.Count <= lower.AutomaticPromotionPlaces)
            return;

        var upperClub = upperTable[upperPosition].Club;
        var lowerClub = lowerTable[lower.AutomaticPromotionPlaces].Club;

        var nextSaturday = State.Date;
        while (nextSaturday.DayOfWeek != DayOfWeek.Saturday)
            nextSaturday = nextSaturday.AddDays(1);

        var nextTuesday = nextSaturday.AddDays(3);
        var playoffRound = (lower.Fixtures.Any() ? lower.Fixtures.Max(p => p.Round) : 0) + 1;

        // The club from the league above is at home first.
        var firstLeg = new FixtureModel
        {
            HomeClub = upperClub,
            AwayClub = lowerClub,
            Round = playoffRound,
            Date = nextSaturday,
            KickOffTime = new TimeOnly(15, 00),
            FixtureType = FixtureType.Playoff
        };

        var secondLeg = new FixtureModel
        {
            HomeClub = lowerClub,
            AwayClub = upperClub,
            Round = playoffRound,
            Date = nextTuesday,
            KickOffTime = new TimeOnly(19, 45),
            FixtureType = FixtureType.PlayoffSecondLeg
        };

        lower.Fixtures.Add(firstLeg);
        lower.Fixtures.Add(secondLeg);

        CompetitionEndEvent.Schedule(EventManager, ServiceProvider, nextTuesday, upper.Id);
        CompetitionEndEvent.Schedule(EventManager, ServiceProvider, nextTuesday, lower.Id);

        var message = $"{upperClub.Name} ({upper.Name}) will face {lowerClub.Name} ({lower.Name}) in the relegation playoff, with a place in {upper.Name} at stake:\n" +
            $"1st Leg: {upperClub.Name} v {lowerClub.Name}, {nextSaturday:d MMMM yyyy}\n" +
            $"2nd Leg: {lowerClub.Name} v {upperClub.Name}, {nextTuesday:d MMMM yyyy}";

        NotificationFactory.AddNotification(
            State.Date,
            "League",
            $"{upper.Name} Relegation Playoff",
            message);
    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        return (true, "");
    }

    // The league above, when this league's play-off place is a relegation play-off against it.
    public static LeagueModel? LeagueAbove(IState state, LeagueModel league) =>
        state.Competitions
            .OfType<LeagueModel>()
            .FirstOrDefault(p => p.CountryId == league.CountryId && p.Rank == league.Rank - 1 && p.RelegationPlayoffPlaces > 0);

    // Draws each tie the day after both leagues have finished.
    public static void ScheduleForLeagues(IState state, IEventManager eventManager, IServiceProvider serviceProvider)
    {
        foreach (var lower in state.Competitions.OfType<LeagueModel>().Where(p => p.PlayoffPlaces > 0))
        {
            if (LeagueAbove(state, lower) is not { } upper)
                continue;

            var lastBaseFixtureDate = upper.Fixtures
                .Concat(lower.Fixtures)
                .Where(p => p.FixtureType == FixtureType.Base)
                .Max(p => p.Date);

            var playoffEvent = ActivatorUtilities.CreateInstance<RelegationPlayoffEvent>(
                serviceProvider, lastBaseFixtureDate.AddDays(1), false, lower.Id);
            eventManager.ValidateAndAddEvent(playoffEvent);
        }
    }
}
