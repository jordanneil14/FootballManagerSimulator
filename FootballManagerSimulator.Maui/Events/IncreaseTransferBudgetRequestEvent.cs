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
        var fullAmount = TransferBudgetRequestRules.AmountForLeagueRank(leagueRank);
        var confidence = State.BoardConfidence;

        if (confidence < BoardConfidenceRules.StableFrom)
        {
            NotificationFactory.AddNotification(
                State.Date,
                "Chairman",
                "Budget Request Denied",
                "The board doesn't feel results justify extra spending right now. Improve your form and try again.");
            return;
        }

        var amount = confidence >= BoardConfidenceRules.SecureFrom ? fullAmount : fullAmount / 2;
        myClub.TransferBudget += amount;

        var message = confidence >= BoardConfidenceRules.SecureFrom
            ? $"The chairman is delighted with your progress and has granted you an extra £{amount:n0} to spend."
            : $"The chairman has partially approved your request and has granted you £{amount:n0}. Continued good results could unlock the full amount next time.";

        NotificationFactory.AddNotification(State.Date, "Chairman", "Budget Request", message);
    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        var recentRequest = State.Events.OfType<IncreaseTransferBudgetRequestEvent>().Any(p => p.TriggerDate > State.Date.AddMonths(-3));
        return (
            !recentRequest,
            "You must wait at least 3 months before re-requesting a higher budget");
    }
}
