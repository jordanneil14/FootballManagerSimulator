using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json;

namespace FootballManagerSimulator.Screens.MenuScreens;

public class LoadGameScreen(
    IState state) : MenuBaseScreen
{
    private readonly List<LoadGamePreview> Games = [];
    private readonly IState State = state;

    public override ScreenType Screen => ScreenType.LoadGame;

    public override Dictionary<string, string> Options => new() { };

	public override string? OptionPrompt => "Enter id to load game save: ";

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
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.Welcome
                });
                break;
            default:
                if (input.All(char.IsNumber) && Games.Count >= int.Parse(input))
                {
                    var game = Games.ElementAt(int.Parse(input) - 1);
                    if (game == null) return;
                    TryLoadGame(game.FileName);
                    State.ScreenStack.Clear();
                    State.ScreenStack.Push(new Screen
                    {
                        Type = ScreenType.Main
                    });
                }
                break;
        }
    }

    private void TryLoadGame(string fileName)
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        try
        {
            var fileContent = File.ReadAllText(path + $"\\{fileName}");
            var s = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto };
            var deserialisedState = JsonConvert.DeserializeObject<State>(fileContent, s);
            if (deserialisedState == null)
                throw new Exception("Unable to load game");

            State.Weather = deserialisedState.Weather;
            State.ScreenStack = deserialisedState.ScreenStack;
            State.Notifications = deserialisedState.Notifications;
            State.ManagerName = deserialisedState.ManagerName;
            State.Clubs = deserialisedState.Clubs;
            State.Date = deserialisedState.Date;
            State.MyClubId = deserialisedState.MyClubId;
            State.Players = deserialisedState.Players;
            State.Competitions = deserialisedState.Competitions;
            State.UserFeedbackUpdates = deserialisedState.UserFeedbackUpdates;
            State.TransferListItems = deserialisedState.TransferListItems;
        }
        catch (Exception ex)
        {
            State.UserFeedbackUpdates.Add(ex.Message);
        }
    }

	public override void RenderTop()
	{
		Console.WriteLine("Load Game");
	}

	public override void RenderSubscreen()
	{
		Games.Clear();
		var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
		var directoryInfo = new DirectoryInfo(path);
		var files = directoryInfo.GetFiles("*.fms");

		foreach (var file in files)
		{
			try
			{
				var fileContents = File.ReadAllText(file.FullName);
				var deserialisedContent = JsonConvert.DeserializeObject<PreviewModel>(fileContents);
				if (deserialisedContent == null) continue;
				Games.Add(new LoadGamePreview
				{
					FileName = file.Name,
					ClubName = deserialisedContent.Club.Name,
					SaveDate = file.LastWriteTime
				});
			}
			catch (Exception)
			{
				//Ignore and move to the next file
			}
		}

		if (Games.Count == 0)
		{
			Console.WriteLine("No game files found on your desktop");
			return;
		}

		Console.WriteLine(string.Format("{0,-10}{1,-30}{2,-30}{3,-20}", "Number", "File Name", "Club Managed", "Last Modified"));
		for (var i = 0; i < Games.Count; i++)
		{
			Console.WriteLine(string.Format("{0,-10}{1,-30}{2,-30}{3,-20}", i + 1, Games.ElementAt(i).FileName, Games.ElementAt(i).ClubName, Games.ElementAt(i).SaveDate));
		}
	}
}


