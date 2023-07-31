using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Factories;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json;
using static FootballManagerSimulator.Interfaces.IState;

namespace FootballManagerSimulator.Screens;

public class LoadGameScreen : IBaseScreen
{
    private readonly IState State;
    private readonly List<LoadGamePreview> Games = new();
    private readonly IUtils HelperFunction;
    private readonly IEnumerable<ICompetitionFactory> LeagueFactories;

    public LoadGameScreen(IState state, IUtils helperFunction, IEnumerable<ICompetitionFactory> leagueFactories)
    {
        State = state;
        LeagueFactories = leagueFactories; 
        HelperFunction = helperFunction;
    }

    public ScreenType Screen => ScreenType.LoadGame;

    public void HandleInput(string input)
    {
        switch(input)
        {
            case "A":
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
                }
                break;
        }
    }

    private void TryLoadGame(string fileName)
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var fileContent = "";

        try
        {
            fileContent = File.ReadAllText(path + $"\\{fileName}");

            var deserialisedState = JsonConvert.DeserializeObject<SerialisableStateModel>(fileContent);

            var playerItems = HelperFunction.GetResource<IEnumerable<Player.SerialisablePlayerModel>>("playersImproved.json");

            State.Teams = HelperFunction.GetResource<IEnumerable<Team>>("teams.json");
            State.Players = deserialisedState.Players
                .Select(p => Player.FromPlayerItem(p, p.ShirtNumber, p.Contract?.TeamID == null ? null : HelperFunction.GetTeam(p.Contract.TeamID.Value))).ToList();
            State.Date = deserialisedState.Date;
            State.Events = deserialisedState.Events;

            foreach (var competition in deserialisedState.Competitions)
            {
                var serialisableCompetitionModel = competition.ToObject<SerialisableCompetitionModel>();
                var competitions = deserialisedState.Competitions.Select(p => LeagueFactories.First(p => serialisableCompetitionModel.CompetitionType == p.CompetitionType).Deserialise(p)).ToList();
                State.Competitions.AddRange(competitions);
            }
            State.Weather = deserialisedState.Weather;
            State.ManagerName = deserialisedState.ManagerName;
            State.Notifications = deserialisedState.Notifications;
            State.MyTeam = HelperFunction.GetTeam(deserialisedState.MyTeam.ID);
            State.ScreenStack.Clear();
            State.ScreenStack.Push(new Screen()
            {
                Type = ScreenType.Main
            });
        }
        catch (Exception)
        {
            State.UserFeedbackUpdates.Add("Unable to load game");
        }
    }

    public void RenderScreen()
    {
        Games.Clear();
        var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var directoryInfo = new DirectoryInfo(path);
        var files = directoryInfo.GetFiles("*.fms");

        foreach(var file in files)
        {
            try
            {
                var fileContents = File.ReadAllText(file.FullName);
                var deserialisedContent = JsonConvert.DeserializeObject<PreviewModel>(fileContents);
                if (deserialisedContent == null) continue;
                Games.Add(new LoadGamePreview
                {
                    FileName = file.Name,
                    ClubName = deserialisedContent.MyTeam.Name,
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
            Console.WriteLine("No game files found");
            return;
        }

        Console.WriteLine(string.Format("{0,-10}{1,-30}{2,-30}{3,-20}", "Number", "File Name", "Club Managed", "Last Modified"));
        for(int i = 0; i < Games.Count; i++)
        {
            Console.WriteLine(string.Format("{0,-10}{1,-30}{2,-30}{3,-20}", i+1, Games.ElementAt(i).FileName, Games.ElementAt(i).ClubName, Games.ElementAt(i).SaveDate));
        }
        Console.WriteLine("\nOptions:");
        Console.WriteLine("A) Back");
        Console.WriteLine("<Enter Number>) Load Game");
    }
}


