using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json;

namespace FootballManagerSimulator.Screens;

public class LoadGameScreen(
    IState state) : IBaseScreen
{
    private readonly List<LoadGamePreview> Games = new List<LoadGamePreview>();

    public ScreenType Screen => ScreenType.LoadGame;

    public void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                state.ScreenStack.Clear();
                state.ScreenStack.Push(new Screen
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
                    state.ScreenStack.Clear();
                    state.ScreenStack.Push(new Screen()
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

            state.Weather = deserialisedState.Weather;
            state.ScreenStack = deserialisedState.ScreenStack;
            state.Notifications = deserialisedState.Notifications;
            state.ManagerName = deserialisedState.ManagerName;
            state.Clubs = deserialisedState.Clubs;
            state.Date = deserialisedState.Date;
            state.MyClubId = deserialisedState.MyClubId;
            state.Players = deserialisedState.Players;
            state.Competitions = deserialisedState.Competitions;
            state.UserFeedbackUpdates = deserialisedState.UserFeedbackUpdates;
            state.TransferListItems = deserialisedState.TransferListItems;
        }
        catch (Exception ex)
        {
            state.UserFeedbackUpdates.Add(ex.Message);
        }
    }

    public void RenderScreen()
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

        Console.WriteLine("Load Game\n");

        if (!Games.Any())
        {
            Console.WriteLine("No game files found on your desktop");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("B) Back");
            return;
        }

        Console.WriteLine(string.Format("{0,-10}{1,-30}{2,-30}{3,-20}", "Number", "File Name", "Club Managed", "Last Modified"));
        for (var i = 0; i < Games.Count; i++)
        {
            Console.WriteLine(string.Format("{0,-10}{1,-30}{2,-30}{3,-20}", i + 1, Games.ElementAt(i).FileName, Games.ElementAt(i).ClubName, Games.ElementAt(i).SaveDate));
        }
        Console.WriteLine("\nOptions:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Number>) Load Game");
    }
}


