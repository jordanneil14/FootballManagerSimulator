using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IState
{
    string ManagerName { get; set; }
    CurrentScreen CurrentScreen { get; set; }
    DateOnly Date { get; set; }
    IEnumerable<Event> Events { get; set; }
    Team MyTeam { get; set; }
    List<Notification> Notifications { get; set; }
    List<Player> Players { get; set; }
    IEnumerable<Team> Teams { get; set; }
    List<ICompetition> Competitions { get; set; }
    List<string> UserFeedbackUpdates { get; set; }
    IEnumerable<Fixture> TodaysFixtures { get => Competitions.SelectMany(p => p.Fixtures).Where(pp => pp.Date == Date); }
    string Weather { get; set; }
    SerialisableStateModel SerialisableState { get; set; }
    public class SerialisableStateModel
    {
        public DateOnly Date { get; set; }
        public string Weather { get; set; } = "";
        public League League { get; set; } = new League();
        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public IEnumerable<Team.SerialisableTeamModel> Teams { get; set; } = new List<Team.SerialisableTeamModel>();
        public Team.SerialisableTeamModel MyTeam { get; set; } = new Team.SerialisableTeamModel();
        public IEnumerable<Player.SerialisablePlayerModel> Players { get; set; } = new List<Player.SerialisablePlayerModel>();
        public IEnumerable<Fixture.SerialisableFixtureModel> Fixtures { get; set; } = new List<Fixture.SerialisableFixtureModel>();
        public IEnumerable<Event> Events { get; set; } = new List<Event>();
        public string ManagerName { get; set; } = "";

        public IEnumerable<ICompetition> Competitions { get; set; }
    }
}