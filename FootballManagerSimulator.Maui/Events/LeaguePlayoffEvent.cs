using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Maui.Events;

public class LeaguePlayoffEvent(
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
        var competition = State.Competitions.First(p => p.Id == CompetitionId);
        var league = (LeagueModel)competition;

        if (league.PlayoffPlaces <= 0)
            return;

        var table = league.GenerateLeagueTable().ToList();

        var playoffTeams = table
            .Skip(league.AutomaticPromotionPlaces)
            .Take(league.PlayoffPlaces)
            .Select(p => p.Club)
            .ToList();

        if (playoffTeams.Count < 4)
            return;

        var nextSaturday = State.Date;
        while (nextSaturday.DayOfWeek != DayOfWeek.Saturday)
            nextSaturday = nextSaturday.AddDays(1);

        var maxRound = competition.Fixtures.Any() ? competition.Fixtures.Max(p => p.Round) : 0;
        var playoffRound = maxRound + 1;

        var semiFinal1 = new FixtureModel
        {
            HomeClub = playoffTeams[0],
            AwayClub = playoffTeams[^1],
            Round = playoffRound,
            Date = nextSaturday,
            KickOffTime = new TimeOnly(15, 00),
            FixtureType = FixtureType.Playoff
        };

        var semiFinal2 = new FixtureModel
        {
            HomeClub = playoffTeams[1],
            AwayClub = playoffTeams[^2],
            Round = playoffRound,
            Date = nextSaturday,
            KickOffTime = new TimeOnly(15, 00),
            FixtureType = FixtureType.Playoff
        };

        competition.Fixtures.Add(semiFinal1);
        competition.Fixtures.Add(semiFinal2);

        var message = $"The {competition.Name} playoff semi-finals have been drawn:\n" +
            $"{semiFinal1.HomeClub.Name} v {semiFinal1.AwayClub.Name}\n" +
            $"{semiFinal2.HomeClub.Name} v {semiFinal2.AwayClub.Name}\n" +
            $"Fixtures will be played on {nextSaturday:d MMMM yyyy}.";

        NotificationFactory.AddNotification(
            State.Date,
            "League",
            $"{competition.Name} Playoffs",
            message);

        var finalTriggerDate = nextSaturday.AddDays(1);
        var finalEvent = ActivatorUtilities.CreateInstance<LeaguePlayoffFinalEvent>(
            ServiceProvider, finalTriggerDate, false, CompetitionId);
        EventManager.ValidateAndAddEvent(finalEvent);
    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        return (true, "");
    }
}
