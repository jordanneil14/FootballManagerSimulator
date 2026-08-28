using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using System.Diagnostics;

namespace FootballManagerSimulator.Screens.MenuScreens;

public class WelcomeScreen(
    IState state,
    IGameFactory gameFactory) : MenuBaseScreen
{
    private readonly IState State = state;
    private readonly IGameFactory GameFactory = gameFactory;

	public override ScreenType Screen => ScreenType.Welcome;

	public override Dictionary<string, string> Options => new() {
		{ "A", "Start New Game" },
		{ "B", "Load Game" },
		{ "Q", "Quit" }
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
			case "A":
				GameFactory.IntitialiseGameState();
				State.ScreenStack.Push(new Screen
				{
					Type = ScreenType.CreateManager
				});
				break;
			case "B":
				State.ScreenStack.Push(new Screen
				{
					Type = ScreenType.LoadGame
				});
				break;
			case "Q":
				Environment.Exit(0);
				break;
			default:
				break;
		}
	}

	public override void RenderSubscreen()
	{
	}

	public override void RenderTop()
	{
		Console.WriteLine("Welcome to Football Manager Simulator");
	}
}