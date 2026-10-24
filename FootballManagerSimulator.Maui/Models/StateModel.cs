using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Models;

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
    public List<IEvent> Events { get; set; } = [];
    public int MatchSpeedMs { get; set; } = 1000;
    public bool AutoSaveEnabled { get; set; } = true;
    // From kick-off until the day's results are in; never saved, since saves are blocked meanwhile.
    [Newtonsoft.Json.JsonIgnore]
    public bool MatchInProgress { get; set; }
    public string CareerId { get; set; } = "";
    public int BoardConfidence { get; set; } = BoardConfidenceRules.Starting;
    public ManagerHistoryModel ManagerHistory { get; set; } = new ManagerHistoryModel();
}

