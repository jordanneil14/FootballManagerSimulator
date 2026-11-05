using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Services;

// Cups have no table, so the analyst's report names the competition instead of a league position,
// and the opponent's division when it isn't ours.
public static class CupPreMatchReport
{
    public static void Send(IState state, INotificationFactory notificationFactory, FixtureModel fixture)
    {
        var competitionName = state.Competitions.FirstOrDefault(p => p.Fixtures.Contains(fixture))?.Name ?? "cup";
        var situation = $"It's a {competitionName} tie.";

        var opponentId = fixture.HomeClub.Id == state.MyClubId ? fixture.AwayClub.Id : fixture.HomeClub.Id;
        var theirLeague = LeagueOf(state, opponentId);
        var myLeague = LeagueOf(state, state.MyClubId);
        if (theirLeague != null && myLeague != null && theirLeague.Id != myLeague.Id)
        {
            var levels = myLeague.Rank - theirLeague.Rank;
            situation += levels == 0
                ? $" They play in {theirLeague.Name}."
                : $" They play in {theirLeague.Name}, {Math.Abs(levels)} level{(Math.Abs(levels) == 1 ? "" : "s")} {(levels > 0 ? "above" : "below")} us.";
        }

        AnalystReport.Send(state, notificationFactory, fixture, situation);
    }

    private static LeagueModel? LeagueOf(IState state, int? clubId)
    {
        var club = state.Clubs.FirstOrDefault(c => c.Id == clubId);
        return club == null ? null : state.Competitions.OfType<LeagueModel>().FirstOrDefault(l => l.Id == club.LeagueId);
    }
}
