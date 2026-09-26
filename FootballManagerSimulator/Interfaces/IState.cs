using FootballManagerSimulator.GameEvent;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IState
{
    string ManagerName { get; set; }
    DateOnly Date { get; set; }
    string DateFriendly => Date.ToString("dddd, dd MMMM yyyy");
    Stack<ScreenModel> ScreenStack { get; set; }
    List<NotificationModel> Notifications { get; set; }
    List<PlayerModel> Players { get; set; }
    List<ClubModel> Clubs { get; set; }
    List<ICompetition> Competitions { get; set; }
    List<string> UserFeedbackUpdates { get; set; }
    string Weather { get; set; }
    public PreviewModel Preview { get; set; }
    public List<TransferListItemModel> TransferListItems { get; set; }
    int? MyClubId { get; set; }
    List<IEvent> Events { get; set; }
    List<IGameEvent> GameEvents { get; set; }
}