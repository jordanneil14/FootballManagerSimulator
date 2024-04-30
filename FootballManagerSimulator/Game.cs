using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator;

public class Game(
    IEnumerable<IBaseScreen> screens,
    IState state) : IGame
{
    public void Run()
    {
        try
        {
            state.ScreenStack.Push(new Screen
            {
                Type = ScreenType.Welcome
            });

            while (true)
            {
                var peek = state.ScreenStack.Peek();
                var screen = screens.First(s => s.Screen == peek.Type);
                Console.Clear();
                Console.WriteLine("\x1b[3J");
                Console.Clear();
                screen.RenderScreen();
                state.UserFeedbackUpdates.Clear();
                var input = Console.ReadLine();
                screen.HandleInput(input.ToUpper());
            }
        }
        catch(Exception)
        {
            //Environment.Exit(0);
        }
    }
}
