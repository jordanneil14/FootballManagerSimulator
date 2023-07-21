using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json;
using System.Linq.Expressions;

namespace FootballManagerSimulator.Screens;

public class LoadGameScreen : IBaseScreen
{
    private readonly IState State;
    private List<LoadGamePreview> Games = new List<LoadGamePreview>();
    private readonly IHelperFunction HelperFunction;

    public LoadGameScreen(IState state, IHelperFunction helperFunction)
    {
        State = state;
        HelperFunction = helperFunction;
    }

    public ScreenType Screen => ScreenType.LoadGame;

    public void HandleInput(string input)
    {
        switch(input)
        {
            case "A":
                State.CurrentScreen.Type = ScreenType.Welcome;
                break;
            default:
                if (input.All(char.IsNumber) && Games.Count() >= Convert.ToInt32(input))
                {
                    var game = Games.ElementAt(Convert.ToInt32(input) - 1);
                    if (game == null) return;
                    LoadGame(game.FileName);
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

            var deserialisedState = JsonConvert.DeserializeObject<IState.SerialisableStateModel>(fileContent);

            var playerItems = HelperFunction.GetPlayers().ToList();

            State.Teams = HelperFunction.GetTeams();
            State.Players = deserialisedState.Players
                .Select(p => Player.FromPlayerItem(p, p.ShirtNumber, p.Contract?.TeamID == null ? null : HelperFunction.GetTeam(p.Contract.TeamID.Value))).ToList();
            State.CurrentScreen = new CurrentScreen()
            {
                Parameters = null,
                Type = ScreenType.Main
            };
            State.Date = deserialisedState.Date;
            State.Events = deserialisedState.Events;
            State.Competitions.First().Fixtures = deserialisedState.Fixtures.Select(p => new Fixture
            {
                AwayTeam = HelperFunction.GetTeam(p.AwayTeamID),
                HomeTeam = HelperFunction.GetTeam(p.HomeTeamID),
                Concluded = p.Concluded,
                Date = p.Date,
                GoalsAway = p.GoalsAway,
                GoalsHome = p.GoalsHome,
                ID = p.ID,
                WeekNumber = p.WeekNumber
            }).ToList();
            State.Weather = deserialisedState.Weather;
            State.ManagerName = deserialisedState.ManagerName;
            State.Notifications = deserialisedState.Notifications;
            State.MyTeam = HelperFunction.GetTeam(deserialisedState.MyTeam.ID);
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
                var deserialisedContent = JsonConvert.DeserializeObject<State>(fileContents);
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

            }
        }

        Console.WriteLine("Load Game\n");

        if (!Games.Any())
        {
            Console.WriteLine("No game files found");
            return;
        }

        Console.WriteLine(string.Format("{0, -10}{1, -30}{2, -30}{3, -20}", "Number", "File Name", "Club Managed", "Last Modified"));
        for(int i = 0; i < Games.Count; i++)
        {
            Console.WriteLine(string.Format("{0,-10}{1,-30}{2,-30}{3,-20}", i+1, Games.ElementAt(i).FileName, Games.ElementAt(i).ClubName, Games.ElementAt(i).SaveDate));
        }
        Console.WriteLine("\nOptions:");
        Console.WriteLine("A) Back");
        Console.WriteLine("<Enter Number>) Load Game");
    }
}

public class LoadGamePreview
{
    public DateTime SaveDate { get; set; }
    public string FileName { get; set; } = "";
    public string ClubName { get; set; } = "";

}
