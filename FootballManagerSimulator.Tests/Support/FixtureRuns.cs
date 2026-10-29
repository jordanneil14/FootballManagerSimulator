using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Support;

public static class FixtureRuns
{
    // The most home, or away, league games in a row that any club plays.
    public static int LongestHomeOrAwayRun(IEnumerable<FixtureModel> fixtures)
    {
        var league = fixtures.Where(f => f.FixtureType == FixtureType.Base).OrderBy(f => f.Round).ThenBy(f => f.Date).ToList();
        var clubIds = league.SelectMany(f => new[] { f.HomeClub.Id, f.AwayClub.Id }).Distinct();

        return clubIds.Max(clubId =>
        {
            var venues = league
                .Where(f => f.HomeClub.Id == clubId || f.AwayClub.Id == clubId)
                .Select(f => f.HomeClub.Id == clubId)
                .ToList();

            var longest = 1;
            var run = 1;
            for (var i = 1; i < venues.Count; i++)
            {
                run = venues[i] == venues[i - 1] ? run + 1 : 1;
                longest = Math.Max(longest, run);
            }
            return longest;
        });
    }
}
