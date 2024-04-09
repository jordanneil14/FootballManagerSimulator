using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IState
{
    string ManagerName { get; set; }
    DateOnly Date { get; set; }
    Stack<Screen> ScreenStack { get; set; }
    Club MyClub { get; set; }
    List<Notification> Notifications { get; set; }
    List<Player> Players { get; set; }
    IEnumerable<Club> Clubs { get; set; }
    List<League> Leagues { get; set; }
    List<string> UserFeedbackUpdates { get; set; }
    List<CompetitionFixture> TodaysFixtures { get; }
    string Weather { get; set; }
    public PreviewModel Preview { get; set; }
    public List<TransferListItem> TransferListItems { get; set; }
}