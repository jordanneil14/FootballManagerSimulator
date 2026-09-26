using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events
{
    public class IncreaseTransferBudgetRequestGameEvent(
        IState state,
        INotificationFactory notificationFactory,
        DateOnly triggerDate) : Event(triggerDate)
    {
        private readonly IState State = state;
        private readonly INotificationFactory NotificationFactory = notificationFactory;
        private const int INCREASED_AMOUNT = 10000000;

        public override void Execute()
        {
            State.Clubs.First(p => p.Id == State.MyClubId).TransferBudget += INCREASED_AMOUNT;

            NotificationFactory.AddNotification(
                State.Date,
                "Chairman",
                "Transfer Budget Request",
                $"The chairman has responded to your transfer budget request and has granted you an extra £{INCREASED_AMOUNT:n} to spend.");

        }

        public override (bool success, string errorMessage) ValidateAdd()
        {
            return (
                State.GameEvents.OfType<IncreaseTransferBudgetRequestGameEvent>().Any(p => p.TriggerDate > State.Date.AddMonths(-3)),
                "You must wait at least 3 months before re-requesting a stadium expansion");
        }
    }
}
