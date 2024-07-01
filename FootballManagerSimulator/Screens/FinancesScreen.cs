using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class FinancesScreen(
    IState state,
    INotificationFactory notificationFactory,
    IEnumerable<IEvent> baseEvents) : BaseScreen(state)
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
                var transferBudgetRequestEvent = baseEvents.First(p => p.Event == EventType.RequestHigherTransferBudget);
                transferBudgetRequestEvent.Start();
                break;
            case "D":
                var stadiumExpansionRequestEvent = baseEvents.First(p => p.Event == EventType.RequestStadiumExpansion);
                stadiumExpansionRequestEvent.Start();
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
        Console.WriteLine("D) Expand Stadium Capacity by 20%");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Finances\n");

        Console.WriteLine($"Transfer budget: {state.Clubs.First(p => p.Id == state.MyClubId).TransferBudgetFriendly}");
    }
}
