using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class FinancesScreen : BaseScreen
{
    private readonly IState State;
    private readonly INotificationFactory NotificationFactory;

    public FinancesScreen(
        IState state,
        INotificationFactory notificationFactory) : base(state)
    {
        State = state;
        NotificationFactory = notificationFactory;
    }

    public override ScreenType Screen => ScreenType.Finances;

    public override void HandleInput(string input)
    {
        switch(input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            case "C":
                State.UserFeedbackUpdates.Add("Transfer budget request has been submitted");
                NotificationFactory.AddNotification(
                    State.Date.AddDays(2),
                    "Chairman",
                    "Extended Transfer Budget Request",
                    $"Your request for the transfer budget to be extended from {State.MyClub.TransferBudgetFriendly} has been rejected. We feel that the current allowance is enough for you to achieve your goals this season");
                break;
            default:
                break;
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("C) Request a Higher Transfer Budget");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Finances\n");

        Console.WriteLine($"Transfer budget: {State.MyClub.TransferBudgetFriendly}");
        Console.WriteLine($"Wage Budget: {State.MyClub.WageBudgetFriendly}");
    }
}
