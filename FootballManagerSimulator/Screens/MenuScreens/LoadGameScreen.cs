using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Factories;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace FootballManagerSimulator.Screens.MenuScreens;

public class LoadGameScreen(
    IState state,
	IServiceProvider serviceProvider,
	IEventManager eventManager) : MenuBaseScreen
{
    private readonly List<LoadGamePreviewModel> Games = [];
    private readonly IState State = state;
	private readonly IServiceProvider ServiceProvider = serviceProvider;
	private readonly IEventManager EventManager = eventManager;

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
                State.ScreenStack.Push(new ScreenModel
                {
                    Type = ScreenType.Welcome
                });
                break;
            default:
                if (input.All(char.IsNumber) && Games.Count >= int.Parse(input) && int.Parse(input) > 0)
                {
                    var game = Games.ElementAt(int.Parse(input) - 1);
                    if (game == null) return;
                    TryLoadGame(game.FileName);
                    State.ScreenStack.Clear();
                    State.ScreenStack.Push(new ScreenModel
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
            var deserialisedState = JsonConvert.DeserializeObject<StateModel>(fileContent, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
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

			foreach (var @event in deserialisedState.GameEvents)
			{
				var gameEvent = (IEvent)ActivatorUtilities.CreateInstance(
					ServiceProvider,
					@event.GetType(),
					@event.TriggerDate,
					@event.IsCompleted
				);
				EventManager.ValidateAndAddEvent(gameEvent);
			}
		}
        catch (Exception ex)
        {
            State.UserFeedbackUpdates.Add(ex.Message);
        }
    }

	public override void RenderTop()
	{
		Console.WriteLine("Load Game\n");
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
				var deserialisedState = JsonConvert.DeserializeObject<StateModel>(fileContents, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
				if (deserialisedState == null)
					continue;

				var clubs = deserialisedState.Clubs;
				var myClubId = deserialisedState.MyClubId;
				var managerName = deserialisedState.ManagerName;

				var myClub = clubs.FirstOrDefault(p => p.Id == myClubId);

				Games.Add(new LoadGamePreviewModel
				{
					FileName = file.Name,
					ClubName = myClub?.Name ?? "",
					ManagerName = deserialisedState.ManagerName,
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

		Console.WriteLine(string.Format("{0,-10}{1,-25}{2,-25}{3,-25}{4,-20}", "Id", "File Name", "Club Managed", "Manager Name",  "Last Modified"));
		for (var i = 0; i < Games.Count; i++)
		{
			var element = Games.ElementAt(i);
			Console.WriteLine($"{i + 1,-10}{element.FileName,-25}{element.ClubName,-25}{element.ManagerName,-25}{element.SaveDate,-20}");
		}
	}
}


