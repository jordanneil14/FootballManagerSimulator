using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Maui.Events;

public class NationalLeaguePlayoffSemiFinalEvent(
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

        var eliminators = competition.Fixtures
            .Where(p => p.FixtureType == FixtureType.Playoff && p.Concluded)
            .ToList();

        if (eliminators.Count < 2)
            return;

        var table = league.GenerateLeagueTable().ToList();
        var second = table.Skip(league.AutomaticPromotionPlaces).First().Club;
        var third = table.Skip(league.AutomaticPromotionPlaces + 1).First().Club;

        var eliminator1Winner = eliminators
            .First(p => p.HomeClub.Id == table.Skip(league.AutomaticPromotionPlaces + 3).First().Club.Id);
        var eliminator2Winner = eliminators
            .First(p => p.HomeClub.Id == table.Skip(league.AutomaticPromotionPlaces + 2).First().Club.Id);

        var winner56 = eliminator1Winner.ClubWon ?? (eliminator1Winner.GoalsHome > eliminator1Winner.GoalsAway ? eliminator1Winner.HomeClub : eliminator1Winner.AwayClub);
        var winner47 = eliminator2Winner.ClubWon ?? (eliminator2Winner.GoalsHome > eliminator2Winner.GoalsAway ? eliminator2Winner.HomeClub : eliminator2Winner.AwayClub);

        var nextTuesday = State.Date;
        while (nextTuesday.DayOfWeek != DayOfWeek.Tuesday)
            nextTuesday = nextTuesday.AddDays(1);

        var maxRound = competition.Fixtures.Max(p => p.Round);

        var semi1 = new FixtureModel
        {
            HomeClub = second,
            AwayClub = winner56,
            Round = maxRound + 1,
            Date = nextTuesday,
            KickOffTime = new TimeOnly(19, 45),
            FixtureType = FixtureType.Playoff
        };

        var semi2 = new FixtureModel
        {
            HomeClub = third,
            AwayClub = winner47,
            Round = maxRound + 1,
            Date = nextTuesday,
            KickOffTime = new TimeOnly(19, 45),
            FixtureType = FixtureType.Playoff
        };

        competition.Fixtures.Add(semi1);
        competition.Fixtures.Add(semi2);

        var message = $"The {competition.Name} play-off semi-finals:\n" +
            $"{semi1.HomeClub.Name} v {semi1.AwayClub.Name}\n" +
            $"{semi2.HomeClub.Name} v {semi2.AwayClub.Name}\n" +
            $"Fixtures will be played on {nextTuesday:d MMMM yyyy}";

        NotificationFactory.AddNotification(
            State.Date,
            "League",
            $"{competition.Name} Play-offs",
            message);

        var finalTriggerDate = nextTuesday.AddDays(1);
        var finalEvent = ActivatorUtilities.CreateInstance<NationalLeaguePlayoffFinalEvent>(
            ServiceProvider, finalTriggerDate, false, CompetitionId);
        EventManager.ValidateAndAddEvent(finalEvent);
    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        return (true, "");
    }
}
