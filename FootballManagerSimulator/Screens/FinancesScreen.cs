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

    public override Dictionary<string, string> Options => new() { 
        { "C", "Request a Higher Transfer Budget" },
        { "D", "Request Stadium Expansion" }
    };

	public override string? OptionPrompt => null;

	public override void HandleInput(string input)
    {
        switch (input)
        {
			case "UPARROW":
				if (base.OptionIndex > 0)
					base.OptionIndex -= 1;
				break;
			case "DOWNARROW":
				if (Options.Count > 1 && base.OptionIndex < Options.Count - 1)
					base.OptionIndex += 1;
				break;
			case "ESCAPE":
				State.ScreenStack.Pop();
				OptionIndex = 0;
				break;
			case "ENTER":
				HandleEnterPress();
				break;
            default:
                break;
        }
    }

    private void HandleEnterPress()
    {
        var option = Options.ElementAt(base.OptionIndex).Key;
        switch (option)
        {
			case "B":
				State.ScreenStack.Pop();
				break;
			case "C":
				var requestHigherTransferVudgetEvent = EventFactories.First(p => p.Type == EventType.RequestHigherTransferBudget);
				requestHigherTransferVudgetEvent.CreateEvent();
				State.UserFeedbackUpdates.Add("Transfer budget request has been submitted");
				break;
			case "D":
				var stadiumExpansionEvent = EventFactories.First(p => p.Type == EventType.RequestStadiumExpansion);
				stadiumExpansionEvent.CreateEvent();
				State.UserFeedbackUpdates.Add("Stadium expansion request has been submitted");
				break;
		}
    }


	public override void RenderSubscreen()
    {
        Console.WriteLine("Finances\n");

        Console.WriteLine($"Transfer budget: {State.Clubs.First(p => p.Id == State.MyClubId).TransferBudgetFriendly}");
    }
}
