using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Events;

// Runs the day after the last of the three meetings: splits the table into a top and bottom
// half, draws the final round within each half, and schedules the end-of-season review.
public class ScottishPremiershipSplitEvent(
    IState state,
    INotificationFactory notificationFactory,
    IEventManager eventManager,
    IServiceProvider serviceProvider,
    ScottishPremiershipService scottishPremiershipService,
    DateOnly triggerDate,
    bool isCompleted,
    int competitionId) : Event(triggerDate, isCompleted)
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    private readonly IEventManager EventManager = eventManager;
    private readonly IServiceProvider ServiceProvider = serviceProvider;
    private readonly ScottishPremiershipService ScottishPremiershipService = scottishPremiershipService;
    public int CompetitionId { get; } = competitionId;

    public override void Execute()
    {
        if (State.Competitions.FirstOrDefault(p => p.Id == CompetitionId) is not LeagueModel league || league.TopSplitClubIds.Count > 0)
            return;

        var lastMatchDate = ScottishPremiershipService.GenerateSplitFixtures(league);
        CompetitionEndEvent.Schedule(EventManager, ServiceProvider, lastMatchDate, CompetitionId);

        var table = league.GenerateLeagueTable().Select(p => p.Club).ToList();
        var top = table.Where(p => league.TopSplitClubIds.Contains(p.Id)).Select(p => p.Name);
        var bottom = table.Where(p => !league.TopSplitClubIds.Contains(p.Id)).Select(p => p.Name);

        NotificationFactory.AddNotification(
            State.Date,
            "League",
            $"{league.Name} Split",
            $"The {league.Name} has split. Each half plays the other clubs in it once more, and no club can move between the halves.\n" +
            $"Top half:\n\t{string.Join("\n\t", top)}\n" +
            $"Bottom half:\n\t{string.Join("\n\t", bottom)}\n" +
            $"The final match is on {lastMatchDate:d MMMM yyyy}");
    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        return (true, "");
    }
}
