using FootballManagerSimulator.Interfaces;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Structures;

public class League : ILeague
{
    public int Id { get; set; }
    public int Rank { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Fixture> Fixtures { get; set; } = new List<Fixture>();
    public List<Club> Clubs { get; set; } = new List<Club>();

    public IOrderedEnumerable<LeaguePositionModel> GenerateLeagueTable()
    {
        var table = new List<LeaguePositionModel>();
        foreach (var club in Clubs)
        {
            var clubFixtures = Fixtures.Where(p => p.Concluded && (p.HomeClub.Id == club.Id || p.AwayClub.Id == club.Id));
            var homeWinPoints = clubFixtures.Where(p => p.HomeClub.Id == club.Id && p.GoalsHome > p.GoalsAway).Count() * 3;
            var awayWinPoints = clubFixtures.Where(p => p.AwayClub.Id == club.Id && p.GoalsAway > p.GoalsHome).Count() * 3;
            var drawPoints = clubFixtures.Where(p => p.GoalsAway == p.GoalsHome).Count();
            var goalsScoredHome = clubFixtures.Where(p => p.HomeClub.Id == club.Id).Sum(p => p.GoalsHome);
            var goalsScoredAway = clubFixtures.Where(p => p.AwayClub.Id == club.Id).Sum(p => p.GoalsAway);

            var goalsConcededHome = clubFixtures.Where(p => p.HomeClub.Id == club.Id).Sum(p => p.GoalsAway);
            var goalsConcededAway = clubFixtures.Where(p => p.AwayClub.Id == club.Id).Sum(p => p.GoalsHome);

            table.Add(new LeaguePositionModel
            {
                ClubName = club.Name,
                Points = homeWinPoints + awayWinPoints + drawPoints,
                GoalsScored = goalsScoredHome.GetValueOrDefault() + goalsScoredAway.GetValueOrDefault(),
                GoalsConceded = goalsConcededHome.GetValueOrDefault() + goalsConcededAway.GetValueOrDefault(),
                Played = clubFixtures.Count()
            });
        }

        return table
            .OrderByDescending(p => p.Points)
            .ThenByDescending(p => p.GoalDifference)
            .ThenBy(p => p.ClubName);
    }
}


