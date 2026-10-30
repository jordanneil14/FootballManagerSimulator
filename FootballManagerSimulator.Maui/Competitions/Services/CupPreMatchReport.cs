using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Services;

// Cups have no table, so the report names the competition and the opposition's best players.
public static class CupPreMatchReport
{
    public static void Send(IState state, INotificationFactory notificationFactory, FixtureModel fixture)
    {
        var oppositionClub = fixture.HomeClub.Id == state.MyClubId ? fixture.AwayClub : fixture.HomeClub;
        var competitionName = state.Competitions.FirstOrDefault(p => p.Fixtures.Contains(fixture))?.Name ?? "cup";

        var oppositionPlayers = state.Players
            .Where(p => p.Contract != null && p.Contract.ClubId == oppositionClub.Id)
            .OrderByDescending(p => p.Rating)
            .Take(3)
            .Select(p => p.Name);

        var message = $"I've generated a pre-match report for the upcoming {competitionName} tie against {oppositionClub.Name}.\n" +
            $"They have numerous players who can cause a threat:\n" +
            $"\t{string.Join("\n\t", oppositionPlayers)}";

        notificationFactory.AddNotification(state.Date, "Club Analyst", "Pre-Match Report", message);
    }
}
