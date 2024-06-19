using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Structures;

public class League : ICompetition
{
    public int Id { get; set; }
    public int Rank { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Fixture> Fixtures { get; set; } = new List<Fixture>();
    public List<Club> Clubs { get; set; } = new List<Club>();

    public string Type => "League";

    public List<DrawDateModel> DrawDates { get; set; } = new List<DrawDateModel>();

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
                Club = club,
                Points = homeWinPoints + awayWinPoints + drawPoints,
                GoalsScored = goalsScoredHome.GetValueOrDefault() + goalsScoredAway.GetValueOrDefault(),
                GoalsConceded = goalsConcededHome.GetValueOrDefault() + goalsConcededAway.GetValueOrDefault(),
                Played = clubFixtures.Count()
            });
        }

        return table
            .OrderByDescending(p => p.Points)
            .ThenByDescending(p => p.GoalsScored - p.GoalsConceded)
            .ThenByDescending(p => p.GoalsScored)
            .ThenBy(p => p.Club.Name);
    }
}


