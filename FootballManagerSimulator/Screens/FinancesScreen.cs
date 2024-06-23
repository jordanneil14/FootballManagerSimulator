using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class FinancesScreen(
    IState state,
    INotificationFactory notificationFactory) : BaseScreen(state)
{
    public override ScreenType Screen => ScreenType.Finances;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                state.ScreenStack.Pop();
                break;
            case "C":
                state.UserFeedbackUpdates.Add("Transfer budget request has been submitted");
                notificationFactory.AddNotification(
                    state.Date.AddDays(2),
                    "Chairman",
                    "Extended Transfer Budget Request",
                    $"Your request for the transfer budget to be extended from {state.Clubs.First(p => p.Id == state.MyClubId).TransferBudgetFriendly} has been rejected. We feel that the current allowance is enough for you to achieve your goals this season");
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

        Console.WriteLine($"Transfer budget: {state.Clubs.First(p => p.Id == state.MyClubId).TransferBudgetFriendly}");
    }
}
