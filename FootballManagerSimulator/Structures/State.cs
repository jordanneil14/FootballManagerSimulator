using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class State : IState
{
    public DateOnly Date { get; set; }
    public string Weather { get; set; } = "";
    public CurrentScreen CurrentScreen { get; set; } = new CurrentScreen() { Type = Enums.ScreenType.Welcome };
    public List<Notification> Notifications { get; set; } = new List<Notification>();
    public IEnumerable<Team> Teams { get; set; } = new List<Team>();
    public Team MyTeam { get; set; } = new Team();
    public List<Player> Players { get; set; } = new List<Player>();
    public List<Fixture> Fixtures { get; set; } = new List<Fixture>();
    public IEnumerable<Event> Events { get; set; } = new List<Event>();
    public List<string> UserFeedbackUpdates { get; set; } = new List<string>();
    public string ManagerName { get; set; } = "";

    // The state has a few self referencing loops which prevents it from being serialised
    // A serialisable version is required to allow saving to a text file
    // If someone knows of a better solution that this please let me know :)
    public IState.SerialisableStateModel SerialisableState { get; set; } = new IState.SerialisableStateModel();
    List<ICompetition> IState.Competitions { get; set; } = new List<ICompetition>();
}
