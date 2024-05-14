using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManagerSimulator.Helpers;

public class NotificationHelper(
    IState state,
    INotificationFactory notificationFactory) : INotificationHelper
{
    public void GeneratePreMatchReportForFixture(Fixture fixture)
    {
        var oppositionClub = fixture.HomeClub.Id == state.MyClub.Id
            ? state.Clubs.First(p => p.Id == fixture.AwayClub.Id)
            : state.Clubs.First(p => p.Id == fixture.HomeClub.Id);

        var league = state.Competitions.First(p => p.Id == state.MyClub.LeagueId) as League;
        var leagueTable = league.GenerateLeagueTable().ToList();

        var leaguePosition = leagueTable.First(p => p.Club.Id == oppositionClub.Id);
        var leaguePositionIndex = leagueTable.IndexOf(leaguePosition) + 1;

        var oppositionPlayers = state.Players
            .Where(p => p.Contract != null && p.Contract.ClubId == oppositionClub.Id)
            .OrderByDescending(p => p.Rating)
            .Take(3)
            .Select(p => p.Name);

        var message = $"I've generate a pre-match report for the upcoming fixture against {oppositionClub.Name}.\n" +
            $"They sit {NumberHelper.AddOrdinal(leaguePositionIndex)} in the league and have numerous players who can cause a threat:\n" +
            $"\t{string.Join("\n\t", oppositionPlayers)}";

        notificationFactory.AddNotification(
            state.Date,
            "Club Analyst",
            "Pre-Match Report",
            message);
    }
}

public interface INotificationHelper
{
    void GeneratePreMatchReportForFixture(Fixture fixture);
}
