using FootballManagerSimulator.Interfaces;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Structures;

public class League : ICompetition
{
    public string Name { get; set; } = "";
    public string CompetitionType { get; set; } = "";
    public List<Fixture> Fixtures { get; set; } = new List<Fixture>();
    public IEnumerable<Team> Teams { get; set; } = new List<Team>();

    public JObject SerialisableCompetition()
    {
        var serialisableCompetitionModel = new SerialisableCompetitionModel()
        {
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
                HomeTeamID = p.HomeTeam.ID,
            }).ToList()
        };
        return JObject.FromObject(serialisableCompetitionModel);
    }

    public IOrderedEnumerable<LeaguePositionModel> GenerateLeagueTable()
    {
        var table = new List<LeaguePositionModel>();
        foreach (var team in Teams)
        {
            var teamFixtures = Fixtures.Where(p => p.Concluded && (p.HomeTeam == team || p.AwayTeam == team));
            var homeWinPoints = teamFixtures.Where(p => p.HomeTeam == team && p.GoalsHome > p.GoalsAway).Count() * 3;
            var awayWinPoints = teamFixtures.Where(p => p.AwayTeam == team && p.GoalsAway > p.GoalsHome).Count() * 3;
            var drawPoints = teamFixtures.Where(p => p.GoalsAway == p.GoalsHome).Count();

            table.Add(new LeaguePositionModel
            {
                TeamName = team.Name,
                Points = homeWinPoints + awayWinPoints + drawPoints
            });
        }
        return table.OrderByDescending(p => p.Points);
    }
}


