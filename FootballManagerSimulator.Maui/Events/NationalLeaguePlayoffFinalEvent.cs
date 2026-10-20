using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Events;

public class NationalLeaguePlayoffFinalEvent(
    IState state,
    INotificationFactory notificationFactory,
    DateOnly triggerDate,
    bool isCompleted,
    int competitionId) : Event(triggerDate, isCompleted)
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    public int CompetitionId { get; } = competitionId;

    public override void Execute()
    {
        var competition = State.Competitions.First(p => p.Id == CompetitionId);

        var maxPlayoffRound = competition.Fixtures
            .Where(p => p.FixtureType == FixtureType.Playoff)
            .Max(p => p.Round);

        var semiFinals = competition.Fixtures
            .Where(p => p.FixtureType == FixtureType.Playoff && p.Round == maxPlayoffRound && p.Concluded)
            .ToList();

        if (semiFinals.Count < 2)
            return;

        var winners = semiFinals
            .Select(p => p.ClubWon ?? (p.GoalsHome > p.GoalsAway ? p.HomeClub : p.AwayClub))
            .ToList();

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

        var message = $"The {competition.Name} promotion final has been set:\n" +
            $"{final.HomeClub.Name} v {final.AwayClub.Name}\n" +
            $"The match will be played on {nextSaturday:d MMMM yyyy}";

        NotificationFactory.AddNotification(
            State.Date,
            "League",
            $"{competition.Name} Promotion Final",
            message);
    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        return (true, "");
    }
}
