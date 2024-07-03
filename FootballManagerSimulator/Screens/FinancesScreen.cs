using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class FinancesScreen(
    IState state,
    IEnumerable<IEventFactory> eventFactories) : BaseScreen(state)
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
                var ev = eventFactories.First(p => p.Type == EventType.RequestHigherTransferBudget);
                ev.CreateEvent();
                state.UserFeedbackUpdates.Add("Transfer budget request has been submitted");

                break;
            case "D":
                var stadiumExpansionEvent = eventFactories.First(p => p.Type == EventType.RequestStadiumExpansion);
                stadiumExpansionEvent.CreateEvent();
                state.UserFeedbackUpdates.Add("Stadium expansion request has been submitted");
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
        Console.WriteLine("D) Request Stadium Expansion");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Finances\n");

        Console.WriteLine($"Transfer budget: {state.Clubs.First(p => p.Id == state.MyClubId).TransferBudgetFriendly}");
    }
}
