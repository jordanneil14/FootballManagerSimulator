using FootballManagerSimulator.Interfaces;
using static FootballManagerSimulator.Interfaces.IState;

namespace FootballManagerSimulator.Structures;

public class State : IState
{
    public DateOnly Date { get; set; }
    public string Weather { get; set; } = "";
    public List<Notification> Notifications { get; set; } = new List<Notification>();
    public IEnumerable<Club> Clubs { get; set; } = new List<Club>();
    public Club MyClub { get; set; } = new Club();
    public List<Player> Players { get; set; } = new List<Player>();
    public List<string> UserFeedbackUpdates { get; set; } = new List<string>();
    public string ManagerName { get; set; } = "";

    public List<CompetitionFixture> TodaysFixtures { get => GetTodaysFixtures(); }

    private List<CompetitionFixture> GetTodaysFixtures()
    {
        var fixtureModel = new List<CompetitionFixture>();
        foreach(var league in Leagues)
        {
            var todaysFixturesForLeague = league.Fixtures.Where(p => p.Date == Date).ToList();
            if (todaysFixturesForLeague.Any())
            {
                fixtureModel.Add(new CompetitionFixture()
                {
                    LeagueId = league.Id,
                    Fixtures = league.Fixtures.Where(p => p.Date == Date)
                });
            }
        }
        return fixtureModel;
    }

    public List<League> Leagues { get; set; } = new List<League>();
    public Stack<Screen> ScreenStack { get; set; } = new Stack<Screen>();

    public PreviewModel Preview { get; set; } = new PreviewModel();
}

