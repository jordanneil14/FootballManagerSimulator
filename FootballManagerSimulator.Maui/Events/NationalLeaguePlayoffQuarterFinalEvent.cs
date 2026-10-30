using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Maui.Events;

public class NationalLeaguePlayoffQuarterFinalEvent(
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

        if (league.PlayoffPlaces < 6)
            return;

        var table = league.GenerateLeagueTable().ToList();

        var playoffTeams = table
            .Skip(league.AutomaticPromotionPlaces)
            .Take(league.PlayoffPlaces)
            .Select(p => p.Club)
            .ToList();

        if (playoffTeams.Count < 6)
            return;

        var nextSaturday = State.Date;
        while (nextSaturday.DayOfWeek != DayOfWeek.Saturday)
            nextSaturday = nextSaturday.AddDays(1);

        var maxRound = competition.Fixtures.Any() ? competition.Fixtures.Max(p => p.Round) : 0;
        var playoffRound = maxRound + 1;

        var eliminator1 = new FixtureModel
        {
            HomeClub = playoffTeams[3],
            AwayClub = playoffTeams[4],
            Round = playoffRound,
            Date = nextSaturday,
            KickOffTime = new TimeOnly(15, 00),
            FixtureType = FixtureType.Playoff
        };

        var eliminator2 = new FixtureModel
        {
            HomeClub = playoffTeams[2],
            AwayClub = playoffTeams[5],
            Round = playoffRound,
            Date = nextSaturday,
            KickOffTime = new TimeOnly(15, 00),
            FixtureType = FixtureType.Playoff
        };

        competition.Fixtures.Add(eliminator1);
        competition.Fixtures.Add(eliminator2);

        var message = $"The {competition.Name} play-off eliminators have been drawn:\n" +
            $"{eliminator1.HomeClub.Name} v {eliminator1.AwayClub.Name}\n" +
            $"{eliminator2.HomeClub.Name} v {eliminator2.AwayClub.Name}\n" +
            $"Fixtures will be played on {nextSaturday:d MMMM yyyy}";

        NotificationFactory.AddNotification(
            State.Date,
            "League",
            $"{competition.Name} Play-offs",
            message);

        var semiTriggerDate = nextSaturday.AddDays(1);
        var semiEvent = ActivatorUtilities.CreateInstance<NationalLeaguePlayoffSemiFinalEvent>(
            ServiceProvider, semiTriggerDate, false, CompetitionId);
        EventManager.ValidateAndAddEvent(semiEvent);
    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        return (true, "");
    }
}
