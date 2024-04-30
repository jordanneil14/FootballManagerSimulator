using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class WelcomeScreen(
    IState state) : IBaseScreen
{
    public ScreenType Screen => ScreenType.Welcome;

    public void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                state.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.CreateManager
                });
                break;
            case "B":
                state.ScreenStack.Push(new Screen
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