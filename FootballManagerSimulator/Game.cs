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

    private static string ReadLineOrKey()
    {
		string retString = "";

		int curIndex = 0;
		do
		{
			ConsoleKeyInfo readKeyResult = Console.ReadKey(true);

			if (readKeyResult.Key == ConsoleKey.UpArrow)
			{
				Console.WriteLine();
				return "UPARROW";
			}

			if (readKeyResult.Key == ConsoleKey.DownArrow)
			{
				Console.WriteLine();
				return "DOWNARROW";
			}
			
			if (readKeyResult.Key == ConsoleKey.Enter)
			{
				Console.WriteLine();
				return string.IsNullOrWhiteSpace(retString) ? "ENTER" : retString.ToUpper();
			}

			if (readKeyResult.Key == ConsoleKey.Escape)
			{
				Console.WriteLine();
				return "ESCAPE";
			}

			if (readKeyResult.Key == ConsoleKey.Backspace)
			{
				if (curIndex > 0)
				{
					retString = retString.Remove(retString.Length - 1);
					Console.Write(readKeyResult.KeyChar);
					Console.Write(' ');
					Console.Write(readKeyResult.KeyChar);
					curIndex--;
				}
			}
			else
			{
				retString += readKeyResult.KeyChar;
				Console.Write(readKeyResult.KeyChar);
				curIndex++;
			}
		}
		while (true);
	}

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
				var input = ReadLineOrKey();

				screen.HandleInput(input);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("-----------------------");
            Console.WriteLine("Fatal error has occurred:");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("Application will now close");
            Console.WriteLine("-----------------------");
            Console.ReadKey();
		}
	}
}
