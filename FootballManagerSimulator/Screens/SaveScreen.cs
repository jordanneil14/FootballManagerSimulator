using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json;

namespace FootballManagerSimulator.Screens;

public class SaveScreen : BaseScreen
{
    public override ScreenType Screen => ScreenType.SaveGame;

    private readonly IState State;

    public SaveScreen(IState state) : base(state)
    {
        State = state;
    }

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                State.CurrentScreen.Type = ScreenType.Main;
                break;
            default:
                SaveGame(input);
                State.CurrentScreen.Type = ScreenType.Main;
                break;
        }
    }

    private void SaveGame(string fileName)
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        var serialisableState = new IState.SerialisableStateModel
        {
            Date = State.Date,
            Events = State.Events,
            Competitions = State.Competitions,
            ManagerName = State.ManagerName,
            MyTeam = State.MyTeam.SerialisableTeam(),
            Notifications = State.Notifications,
            Weather = State.Weather,
            Teams = State.Teams.Select(p => p.SerialisableTeam()),
            Players = State.Players.Select(p => p.SerialisablePlayer())
        };

        try 
        {
            var s = JsonConvert.SerializeObject(serialisableState);
            File.WriteAllText(path + $"\\{fileName}.fms", s);
            State.UserFeedbackUpdates.Add("Game saved successfully");
        }
        catch
        {
            State.UserFeedbackUpdates.Add("Unable to save game");
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter file name>) Save Game");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Games will be saved to your desktop");
    }
}
