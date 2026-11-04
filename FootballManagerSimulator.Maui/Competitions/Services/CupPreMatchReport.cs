using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Services;

// Cups have no table, so the analyst's report names the competition instead of a league position.
public static class CupPreMatchReport
{
    public static void Send(IState state, INotificationFactory notificationFactory, FixtureModel fixture)
    {
        var competitionName = state.Competitions.FirstOrDefault(p => p.Fixtures.Contains(fixture))?.Name ?? "cup";
        AnalystReport.Send(state, notificationFactory, fixture, $"It's a {competitionName} tie.");
    }
}
