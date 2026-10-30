using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Events;

public class EnglishFootballLeaguePlayoffFinalEvent(
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

        var secondLegs = competition.Fixtures
            .Where(p => p.FixtureType == FixtureType.PlayoffSecondLeg && p.Concluded)
            .ToList();

        if (secondLegs.Count < 2)
            return;

        var winners = new List<ClubModel>();
        foreach (var secondLeg in secondLegs)
        {
            if (secondLeg.ClubWon != null)
            {
                winners.Add(secondLeg.ClubWon);
            }
            else
            {
                var firstLeg = competition.Fixtures.First(p =>
                    p.FixtureType == FixtureType.Playoff && p.Concluded &&
                    p.HomeClub.Id == secondLeg.AwayClub.Id && p.AwayClub.Id == secondLeg.HomeClub.Id);

                var homeAgg = (secondLeg.GoalsHome ?? 0) + (firstLeg.GoalsAway ?? 0);
                var awayAgg = (secondLeg.GoalsAway ?? 0) + (firstLeg.GoalsHome ?? 0);

                winners.Add(homeAgg > awayAgg ? secondLeg.HomeClub : secondLeg.AwayClub);
            }
        }

        var nextSaturday = State.Date;
        while (nextSaturday.DayOfWeek != DayOfWeek.Saturday)
            nextSaturday = nextSaturday.AddDays(1);

        var maxRound = competition.Fixtures.Max(p => p.Round);

        var final = new FixtureModel
        {
            HomeClub = winners[0],
            AwayClub = winners[1],
            Round = maxRound + 1,
            Date = nextSaturday,
            KickOffTime = new TimeOnly(15, 00),
            FixtureType = FixtureType.PlayoffFinal
        };

        competition.Fixtures.Add(final);
        CompetitionEndEvent.Schedule(EventManager, ServiceProvider, final.Date, CompetitionId);

        var message = $"The {competition.Name} play-off final has been set:\n" +
            $"{final.HomeClub.Name} v {final.AwayClub.Name}\n" +
            $"The match will be played on {nextSaturday:d MMMM yyyy}";

        NotificationFactory.AddNotification(
            State.Date,
            "League",
            $"{competition.Name} Play-off Final",
            message);
    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        return (true, "");
    }
}
