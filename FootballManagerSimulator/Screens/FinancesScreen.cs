using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class FinancesScreen(
    IState state,
    IEnumerable<IEventFactory> eventFactories) : BaseScreen(state)
{
    private readonly IState State = state;
    private readonly IEnumerable<IEventFactory> EventFactories = eventFactories;

    public override ScreenType Screen => ScreenType.Finances;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            case "C":
                var ev = EventFactories.First(p => p.Type == EventType.RequestHigherTransferBudget);
                ev.CreateEvent();
                State.UserFeedbackUpdates.Add("Transfer budget request has been submitted");

                break;
            case "D":
                var stadiumExpansionEvent = EventFactories.First(p => p.Type == EventType.RequestStadiumExpansion);
                stadiumExpansionEvent.CreateEvent();
                State.UserFeedbackUpdates.Add("Stadium expansion request has been submitted");
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

        Console.WriteLine($"Transfer budget: {State.Clubs.First(p => p.Id == State.MyClubId).TransferBudgetFriendly}");
    }
}
