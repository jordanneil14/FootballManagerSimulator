using FootballManagerSimulator.Maui.Events;
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
    int MatchSpeedMs { get; set; }
    bool AutoSaveEnabled { get; set; }
    bool MatchInProgress { get; set; }
    // Ties a career's autosave slot to it across saves and loads.
    string CareerId { get; set; }
    double BoardConfidence { get; set; }
    ManagerHistoryModel ManagerHistory { get; set; }
    // Null until first needed, so old saves start from their club (see ReputationHelper).
    double? ManagerReputation { get; set; }
    double? LastSeasonReputationChange { get; set; }
    List<JobApplicationModel> JobApplications { get; set; }
    List<JobRejectionModel> JobRejections { get; set; }
    List<ScoutModel> Scouts { get; set; }
    List<PhysioModel> Physios { get; set; }
    // At most one (AnalystRules.MaxAnalysts); without one there are no pre-match reports.
    List<AnalystModel> Analysts { get; set; }
    // Players shown on the Scout screen; scouts add to it every month.
    List<int> DiscoveredPlayerIds { get; set; }
    bool ShowTransferNotifications { get; set; }
    bool ShowManagerNotifications { get; set; }
}