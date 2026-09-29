using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Screens;

public class FinancesScreen(
    IState state,
    IServiceProvider serviceProvider,
	IEventManager eventManager) : BaseScreen(state)
{
    private readonly IState State = state;
	private readonly IServiceProvider ServiceProvider = serviceProvider;
	private readonly IEventManager EventManager = eventManager;

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
				var increaseTransferBudgetRequest = ActivatorUtilities.CreateInstance<IncreaseTransferBudgetRequestEvent>(ServiceProvider, State.Date.AddDays(2));
				EventManager.ValidateAndAddEvent(increaseTransferBudgetRequest);
				State.UserFeedbackUpdates.Add("Transfer budget request has been submitted");
				break;
			case "D":
				var stadiumExpansionRequest = ActivatorUtilities.CreateInstance<StadiumExpansionRequestEvent>(ServiceProvider, State.Date.AddDays(2));
				EventManager.ValidateAndAddEvent(stadiumExpansionRequest);
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
