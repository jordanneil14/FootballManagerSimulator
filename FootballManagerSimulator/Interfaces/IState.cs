using FootballManagerSimulator.Structures;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Interfaces;

public interface IState
{
    string ManagerName { get; set; }
    DateOnly Date { get; set; }
    Stack<Screen> ScreenStack { get; set; }
    IEnumerable<Event> Events { get; set; }
    Club MyClub { get; set; }
    List<Notification> Notifications { get; set; }
    List<Player> Players { get; set; }
    IEnumerable<Club> Clubs { get; set; }
    List<ICompetition> Competitions { get; set; }
    List<string> UserFeedbackUpdates { get; set; }
    IEnumerable<CompetitionFixtureModel> TodaysFixtures { get; }
    string Weather { get; set; }
    public SerialisableStateModel SerialisableState { get; set; }
    public class SerialisableStateModel
    {
        public DateOnly Date { get; set; }
        public string Weather { get; set; } = "";
        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public IEnumerable<Club.SerialisableClubModel> Teams { get; set; } = new List<Club.SerialisableClubModel>();
        public Club.SerialisableClubModel MyTeam { get; set; } = new Club.SerialisableClubModel();
        public IEnumerable<Player.SerialisablePlayerModel> Players { get; set; } = new List<Player.SerialisablePlayerModel>();
        public IEnumerable<Event> Events { get; set; } = new List<Event>();
        public string ManagerName { get; set; } = "";
        public IEnumerable<JObject> Competitions { get; set; } = new List<JObject>();
    }
    public PreviewModel Preview { get; set; }
    public class PreviewModel
    {
        public Club.SerialisableClubModel Club { get; set; } = new Club.SerialisableClubModel();
    }
}