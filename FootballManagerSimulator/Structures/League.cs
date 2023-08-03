using FootballManagerSimulator.Interfaces;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Structures;

public class League : ICompetition
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public string CompetitionType { get; set; } = "";
    public List<Fixture> Fixtures { get; set; } = new List<Fixture>();
    public IEnumerable<Club> Teams { get; set; } = new List<Club>();
    public JObject SerialisableCompetition()
    {
        var serialisableCompetitionModel = new SerialisableCompetitionModel()
        {
            ID = ID,
            Name = Name,
            CompetitionType = CompetitionType,
            Fixtures = Fixtures.Select(p => new Fixture.SerialisableFixtureModel
            {
                AwayTeamID = p.AwayTeam.ID,
                WeekNumber = p.WeekNumber,
                Concluded = p.Concluded,
                Date = p.Date,
                ID = p.ID,
                GoalsAway = p.GoalsAway,
                GoalsHome = p.GoalsHome,
                HomeTeamID = p.HomeTeam.ID
            }).ToList()
        };
        return JObject.FromObject(serialisableCompetitionModel);
    }

    public IOrderedEnumerable<LeaguePositionModel> GenerateLeagueTable()
    {
        var table = new List<LeaguePositionModel>();
        foreach (var team in Teams)
        {
            var teamFixtures = Fixtures.Where(p => p.Concluded && (p.HomeTeam.ID == team.ID || p.AwayTeam.ID == team.ID));
            var homeWinPoints = teamFixtures.Where(p => p.HomeTeam.ID == team.ID && p.GoalsHome > p.GoalsAway).Count() * 3;
            var awayWinPoints = teamFixtures.Where(p => p.AwayTeam.ID == team.ID && p.GoalsAway > p.GoalsHome).Count() * 3;
            var drawPoints = teamFixtures.Where(p => p.GoalsAway == p.GoalsHome).Count();
            var goalsScoredHome = teamFixtures.Where(p => p.HomeTeam.ID == team.ID).Sum(p => p.GoalsHome);
            var goalsScoredAway = teamFixtures.Where(p => p.AwayTeam.ID == team.ID).Sum(p => p.GoalsAway);

            var goalsConcededHome = teamFixtures.Where(p => p.HomeTeam.ID == team.ID).Sum(p => p.GoalsAway);
            var goalsConcededAway = teamFixtures.Where(p => p.AwayTeam.ID == team.ID).Sum(p => p.GoalsHome);

            table.Add(new LeaguePositionModel
            {
                TeamName = team.Name,
                Points = homeWinPoints + awayWinPoints + drawPoints,
                GoalsScored = goalsScoredHome.GetValueOrDefault() + goalsScoredAway.GetValueOrDefault(),
                GoalsConceded = goalsConcededHome.GetValueOrDefault() + goalsConcededAway.GetValueOrDefault(),
            });
        }
        return table.OrderByDescending(p => p.Points).ThenByDescending(p => p.GoalDifference).ThenBy(p => p.TeamName);
    }
}


