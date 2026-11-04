using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Services;

// League matches: the analyst's report opens with where the opponent sits in the user's league.
public static class LeaguePreMatchReport
{
    public static void Send(IState state, INotificationFactory notificationFactory, FixtureModel fixture)
    {
        var myClub = state.Clubs.First(p => p.Id == state.MyClubId);
        var opponentId = fixture.HomeClub.Id == myClub.Id ? fixture.AwayClub.Id : fixture.HomeClub.Id;

        var situation = "";
        if (state.Competitions.FirstOrDefault(p => p.Id == myClub.LeagueId) is LeagueModel league)
        {
            var table = league.GenerateLeagueTable().ToList();
            var index = table.FindIndex(p => p.Club.Id == opponentId);
            if (index >= 0)
                situation = $"They sit {NumberHelper.AddOrdinal(index + 1)} in the league.";
        }

        AnalystReport.Send(state, notificationFactory, fixture, situation);
    }
}
