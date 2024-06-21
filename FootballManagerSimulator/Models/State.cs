using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class State : IState
{
    public DateOnly Date { get; set; }
    public string Weather { get; set; } = "";
    public List<Notification> Notifications { get; set; } = new List<Notification>();
    public IEnumerable<Club> Clubs { get; set; } = new List<Club>();
    public int? MyClubId { get; set; }
    public List<Player> Players { get; set; } = new List<Player>();
    public List<string> UserFeedbackUpdates { get; set; } = new List<string>();
    public string ManagerName { get; set; } = "";

    public List<ICompetition> Competitions { get; set; } = new List<ICompetition>();
    public Stack<Screen> ScreenStack { get; set; } = new Stack<Screen>();
    public PreviewModel Preview { get; set; } = new PreviewModel();
    public List<TransferListItem> TransferListItems { get; set; } = new List<TransferListItem>();
}

