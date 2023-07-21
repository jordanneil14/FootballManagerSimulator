using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

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
        State.CurrentScreen.Type = ScreenType.Welcome;

        while (true)
        {
            var screen = Screens.First(s => s.Screen == State.CurrentScreen.Type);
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
