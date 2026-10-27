using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Events;

public class IncreaseTransferBudgetRequestEvent(
    IState state,
    INotificationFactory notificationFactory,
    DateOnly triggerDate,
    bool isCompleted) : Event(triggerDate, isCompleted)
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public override void Execute()
    {
        var myClub = State.Clubs.First(p => p.Id == State.MyClubId);
        var leagueRank = State.Competitions.OfType<LeagueModel>().FirstOrDefault(p => p.Id == myClub.LeagueId)?.Rank;
        var amount = TransferBudgetRequestRules.AmountForLeagueRank(leagueRank);

        myClub.TransferBudget += amount;

        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            "Budget Request",
            $"The chairman has responded to your budget request and has granted you an extra £{amount:n0} to spend");

    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        var recentRequest = State.Events.OfType<IncreaseTransferBudgetRequestEvent>().Any(p => p.TriggerDate > State.Date.AddMonths(-3));
        return (
            !recentRequest,
            "You must wait at least 3 months before re-requesting a higher budget");
    }
}
