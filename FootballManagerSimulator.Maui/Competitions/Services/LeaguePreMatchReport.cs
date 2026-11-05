using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Services;

// League matches: the analyst's report opens with where the opponent sits in the user's league,
// compared with us.
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
            var theirs = table.FindIndex(p => p.Club.Id == opponentId);
            var mine = table.FindIndex(p => p.Club.Id == myClub.Id);
            if (theirs >= 0)
            {
                var points = table[theirs].Points;
                situation = $"They're {NumberHelper.AddOrdinal(theirs + 1)} in the league with {Count(points, "point")}";
                situation += mine >= 0 ? $", {Comparison(mine - theirs, points - table[mine].Points)}." : ".";
            }
        }

        AnalystReport.Send(state, notificationFactory, fixture, situation);
    }

    // places: how many places above us they are (negative below); points: how many points more they have.
    internal static string Comparison(int places, int points)
    {
        var where = places > 0 ? "above" : "below";
        var placeText = Count(Math.Abs(places), "place");
        if (points == 0)
            return $"{placeText} {where} us but level on points";
        // Above us on points too (or below on both): "3 places and 5 points above us".
        if (Math.Sign(points) == Math.Sign(places))
            return $"{placeText} and {Count(Math.Abs(points), "point")} {where} us";
        // Ahead in the table but behind on points: a split league, where the top half stays above the rest.
        return $"{placeText} {where} us";
    }

    private static string Count(int n, string noun) => $"{n} {noun}{(n == 1 ? "" : "s")}";
}
