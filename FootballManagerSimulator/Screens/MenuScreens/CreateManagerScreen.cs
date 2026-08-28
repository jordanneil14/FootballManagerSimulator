using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using System.Globalization;

namespace FootballManagerSimulator.Screens.MenuScreens;

public class CreateManagerScreen(
    IState state,
    IGameCreator gameCreator) : MenuBaseScreen
{
    private readonly IState State = state;
    private readonly IGameCreator GameCreator = gameCreator;

	public override ScreenType Screen => ScreenType.CreateManager;

	public override Dictionary<string, string> Options => new() {

	};

	public override string? OptionPrompt => "Enter your name: ";

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
			default:
				if (string.IsNullOrWhiteSpace(input)) return;
				var text = new CultureInfo("en-US", false).TextInfo;
				GameCreator.ManagerName = text.ToTitleCase(input.ToLower());
				State.ScreenStack.Push(new Screen
				{
					Type = ScreenType.SelectLeague
				});
				break;
		}
	}

	public override void RenderSubscreen()
	{
	}

	public override void RenderTop()
	{
		Console.WriteLine("Create Manager");
	}
}
