using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface IState
{
    string ManagerName { get; set; }
    DateOnly Date { get; set; }
    string DateFriendly => Date.ToString("ddd, dd MMMM yyyy");
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
}