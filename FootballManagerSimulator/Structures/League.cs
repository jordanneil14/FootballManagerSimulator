using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class League : ICompetition
{
    public string Name { get; set; } = "";
    public List<Fixture> Fixtures { get; set; } = new List<Fixture>();
    public IEnumerable<Team> Teams { get => Fixtures.Select(p => p.HomeTeam).Distinct(); }

    public IOrderedEnumerable<LeagueTableModel> LeagueTable { get => GenerateLeagueTable(); }

    public class LeagueTableModel
    {
        public string TeamName { get; set; } = "";
        public int Points { get; set; }
    }

    private IOrderedEnumerable<LeagueTableModel> GenerateLeagueTable()
    {
        var table = new List<LeagueTableModel>();
        foreach(var team in Teams)
        {
            var teamFixtures = Fixtures.Where(p => p.Concluded && (p.HomeTeam == team || p.AwayTeam == team));
            var homeWinPoints = teamFixtures.Where(p => p.HomeTeam == team && p.GoalsHome > p.GoalsAway).Count() * 3;
            var awayWinPoints = teamFixtures.Where(p => p.AwayTeam == team && p.GoalsAway > p.GoalsHome).Count() * 3;
            var drawPoints = teamFixtures.Where(p => p.GoalsAway == p.GoalsHome).Count();

            table.Add(new LeagueTableModel
            {
                TeamName = team.Name,
                Points = homeWinPoints + awayWinPoints + drawPoints
            });
        }
        return table.OrderByDescending(p => p.Points);
    }
}
