using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator;

public class Game : IGame
{
    private readonly IEnumerable<IBaseScreen> Screens;
    private readonly IState State;

    public Game(
        IEnumerable<IBaseScreen> screens,
        IState state)
    {
        Screens = screens;
        State = state;
    }

    public void Run()
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
            screen.RenderScreen();
            var input = Console.ReadLine();
            if (input != null)
            {
                screen.HandleInput(input.ToUpper());
            }
        }
    }
}
