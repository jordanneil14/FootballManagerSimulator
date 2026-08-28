using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens.MenuScreens;

public abstract class MenuBaseScreen : IBaseScreen
{
	public abstract ScreenType Screen { get; }
	public abstract IDictionary<string, string> Options { get; }
	public abstract string? OptionPrompt { get; }
	public int OptionIndex { get; set; }
	public abstract void HandleInput(string input);

	public abstract void RenderTop();
	public abstract void RenderSubscreen();

	public void RenderScreen()
	{
		RenderTop();
		RenderSubscreen();
		Console.WriteLine("\n");
		RenderOptions();
	}

	public void RenderOptions()
	{
		for (var i = 0; i < Options.Count(); i++)
		{
			if (OptionIndex == i)
			{
				Console.WriteLine($"> {Options.ElementAt(i).Value.ToUpper()}");
			}
			else
			{
				Console.WriteLine($"{Options.ElementAt(i).Value}");
			}
		}

		if (!string.IsNullOrWhiteSpace(OptionPrompt))
			Console.Write(OptionPrompt);
	}
}