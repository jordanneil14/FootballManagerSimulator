using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IState
{
    string ManagerName { get; set; }
    DateOnly Date { get; set; }
    Stack<Screen> ScreenStack { get; set; }
    List<Notification> Notifications { get; set; }
    List<Player> Players { get; set; }
    IEnumerable<Club> Clubs { get; set; }
    List<ICompetition> Competitions { get; set; }
    List<string> UserFeedbackUpdates { get; set; }
    string Weather { get; set; }
    public PreviewModel Preview { get; set; }
    public List<TransferListItem> TransferListItems { get; set; }
    int? MyClubId { get; set; }
    List<IEvent> Events { get; set; }
}