using FootballManagerSimulator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Events
{
    public class StadiumExpansionRequestGameEvent(
        IState state,
        INotificationFactory notificationFactory,
        IEventManager gameEventManager,
        DateOnly triggerDate,
        IServiceProvider serviceProvider) : Event(triggerDate)
    {
        private readonly IState State = state;
        private readonly INotificationFactory NotificationFactory = notificationFactory;
        private readonly IEventManager GameEventManager = gameEventManager;
        private readonly IServiceProvider ServiceProvider = serviceProvider;

        public override void Execute()
        {
            var budgetRequired = 2000000;

            var myClub = State.Clubs.First(p => p.Id == State.MyClubId);
            if (myClub.TransferBudget > budgetRequired)
            {
                var completionDate = State.Date.AddMonths(3);

                var gameEvent = ActivatorUtilities.CreateInstance<StadiumExpansionGameEvent>(ServiceProvider, completionDate);
                GameEventManager.ValidateAndAddEvent(gameEvent);

                NotificationFactory.AddNotification(
                    State.Date,
                    "Chairman",
                    "Stadium Expansion Request",
                    $"Your stadium expansion request has been accepted by the owner. Work will begin immediately and finish on {completionDate}");

                myClub.TransferBudget -= budgetRequired;
            }
            else
            {
                NotificationFactory.AddNotification(
                    State.Date,
                    "Chairman",
                    "Stadium Expansion Request",
                    $"Your stadium expansion request has been rejected by the owner. The club finances do not allow this move.");
            }
        }

        public override (bool success, string errorMessage) ValidateAdd()
        {
            return (
                State.GameEvents.OfType<StadiumExpansionRequestGameEvent>().Any(p => p.TriggerDate > State.Date.AddMonths(-3)),
                "You must wait at least 3 months before re-requesting a stadium expansion");
        }
    }
}