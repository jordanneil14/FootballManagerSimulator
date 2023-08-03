using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json;
using static FootballManagerSimulator.Interfaces.IState;

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
        var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        var serialisableState = new SerialisableStateModel
        {
            Date = State.Date,
            Events = State.Events,
            Competitions = State.Competitions.Select(p => p.SerialisableCompetition()),
            ManagerName = State.ManagerName,
            MyTeam = State.MyClub.SerialisableTeam(),
            Notifications = State.Notifications,
            Weather = State.Weather,
            Teams = State.Clubs.Select(p => p.SerialisableTeam()),
            Players = State.Players.Select(p => p.SerialisablePlayer()),
        };

        try 
        {
            var stateAsJson = JsonConvert.SerializeObject(serialisableState);
            File.WriteAllText(path + $"\\{fileName}.fms", stateAsJson);
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
