using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Factories;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Screens;

public class WelcomeScreen(
    IState state,
    IGameFactory gameFactory) : IBaseScreen
{
    private readonly IState State = state;
    private readonly IGameFactory GameFactory = gameFactory;

    public ScreenType Screen => ScreenType.Welcome;

    public void HandleInput(string input)
    {
        switch (input)
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

    public void RenderScreen()
    {
        Console.WriteLine("Welcome to Football Manager Simulator\n");
        Console.WriteLine("Options:");
        Console.WriteLine("A) Start New Game");
        Console.WriteLine("B) Load Game");
        Console.WriteLine("Q) Quit");
    }
}