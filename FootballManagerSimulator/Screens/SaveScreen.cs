using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using Newtonsoft.Json;

namespace FootballManagerSimulator.Screens;

public class SaveScreen(IState state) : BaseScreen(state)
{
    private readonly IState State = state;

    public override ScreenType Screen => ScreenType.SaveGame;

    public override Dictionary<string, string> Options => new() {};

	public override string? OptionPrompt => "Enter file name: ";

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
			case "B":
                State.ScreenStack.Clear();
                break;
            default:
                SaveGame(input);
                break;
        }

        State.ScreenStack.Push(new Screen
        {
            Type = ScreenType.Main
        });
    }

    private void SaveGame(string fileName)
    {
        try
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var stateAsJson = JsonConvert.SerializeObject(State, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            File.WriteAllText(path + $"\\{fileName}.fms", stateAsJson);
            State.UserFeedbackUpdates.Add("Game saved successfully");
        }
        catch (Exception)
        {
            State.UserFeedbackUpdates.Add("Unable to save game");
        }
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Games will be saved to your desktop");
    }
}
