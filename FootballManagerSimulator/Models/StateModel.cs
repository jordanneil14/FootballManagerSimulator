using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Structures;

public class StateModel : IState
{
    public DateOnly Date { get; set; }
    public string Weather { get; set; } = "";
    public List<NotificationModel> Notifications { get; set; } = new List<NotificationModel>();
    public List<ClubModel> Clubs { get; set; } = new List<ClubModel>();
    public int? MyClubId { get; set; }
    public List<PlayerModel> Players { get; set; } = new List<PlayerModel>();
    public List<string> UserFeedbackUpdates { get; set; } = new List<string>();
    public string ManagerName { get; set; } = "";

    public List<ICompetition> Competitions { get; set; } = new List<ICompetition>();
    public Stack<ScreenModel> ScreenStack { get; set; } = new Stack<ScreenModel>();
    public PreviewModel Preview { get; set; } = new PreviewModel();
    public List<TransferListItemModel> TransferListItems { get; set; } = new List<TransferListItemModel>();
    public List<IEvent> Events { get; set; } = new List<IEvent>();
}

