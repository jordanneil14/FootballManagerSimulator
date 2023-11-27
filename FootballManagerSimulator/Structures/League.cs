using FootballManagerSimulator.Interfaces;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Structures;

public class League : ICompetition
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public string CompetitionType { get; set; } = "";
    public List<Fixture> Fixtures { get; set; } = new List<Fixture>();
    public IEnumerable<Club> Clubs { get; set; } = new List<Club>();
    public JObject SerialisableCompetition()
    {
        var serialisableCompetitionModel = new SerialisableCompetitionModel()
        {
            ID = ID,
            Name = Name,
            CompetitionType = CompetitionType,
            Fixtures = Fixtures.Select(p => new Fixture.SerialisableFixtureModel
            {
                AwayClubId = p.AwayClub.ID,
                WeekNumber = p.WeekNumber,
                Concluded = p.Concluded,
                Date = p.Date,
                ID = p.ID,
                GoalsAway = p.GoalsAway,
                GoalsHome = p.GoalsHome,
                HomeClubId = p.HomeClub.ID
            }).ToList()
        };
        return JObject.FromObject(serialisableCompetitionModel);
    }

    public IOrderedEnumerable<LeaguePositionModel> GenerateLeagueTable()
    {
        var table = new List<LeaguePositionModel>();
        foreach (var club in Clubs)
        {
            var clubFixtures = Fixtures.Where(p => p.Concluded && (p.HomeClub.ID == club.ID || p.AwayClub.ID == club.ID));
            var homeWinPoints = clubFixtures.Where(p => p.HomeClub.ID == club.ID && p.GoalsHome > p.GoalsAway).Count() * 3;
            var awayWinPoints = clubFixtures.Where(p => p.AwayClub.ID == club.ID && p.GoalsAway > p.GoalsHome).Count() * 3;
            var drawPoints = clubFixtures.Where(p => p.GoalsAway == p.GoalsHome).Count();
            var goalsScoredHome = clubFixtures.Where(p => p.HomeClub.ID == club.ID).Sum(p => p.GoalsHome);
            var goalsScoredAway = clubFixtures.Where(p => p.AwayClub.ID == club.ID).Sum(p => p.GoalsAway);

            var goalsConcededHome = clubFixtures.Where(p => p.HomeClub.ID == club.ID).Sum(p => p.GoalsAway);
            var goalsConcededAway = clubFixtures.Where(p => p.AwayClub.ID == club.ID).Sum(p => p.GoalsHome);

            table.Add(new LeaguePositionModel
            {
                ClubName = club.Name,
                Points = homeWinPoints + awayWinPoints + drawPoints,
                GoalsScored = goalsScoredHome.GetValueOrDefault() + goalsScoredAway.GetValueOrDefault(),
                GoalsConceded = goalsConcededHome.GetValueOrDefault() + goalsConcededAway.GetValueOrDefault(),
            });
        }
        return table.OrderByDescending(p => p.Points).ThenByDescending(p => p.GoalDifference).ThenBy(p => p.ClubName);
    }
}


