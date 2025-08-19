using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator;

public class Game(
    IEnumerable<IBaseScreen> screens,
    IState state) : IGame
{
    private readonly IEnumerable<IBaseScreen> Screens = screens;
    private readonly IState State = state;

    public void Run()
    {
        try
        {
            State.ScreenStack.Push(new Screen
            {
                Type = ScreenType.Welcome
            });

            while (true)
            {
                var peek = State.ScreenStack.Peek();
                var screen = Screens.First(s => s.Screen == peek.Type);
                Console.Clear();
                Console.WriteLine("\x1b[3J");
                Console.Clear();
                screen.RenderScreen();
                State.UserFeedbackUpdates.Clear();
                var input = Console.ReadLine();
                screen.HandleInput(input.ToUpper());
            }
        }
        catch (Exception)
        {
            //Environment.Exit(0);
        }
    }
}
