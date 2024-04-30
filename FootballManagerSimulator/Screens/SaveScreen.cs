using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json;

namespace FootballManagerSimulator.Screens;

public class SaveScreen(IState state) : BaseScreen(state)
{
    public override ScreenType Screen => ScreenType.SaveGame;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                state.ScreenStack.Clear();
                break;
            default:
                SaveGame(input);
                break;
        }

        state.ScreenStack.Push(new Screen
        {
            Type = ScreenType.Main
        });
    }

    private void SaveGame(string fileName)
    {
        try
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var stateAsJson = JsonConvert.SerializeObject(state);
            File.WriteAllText(path + $"\\{fileName}.fms", stateAsJson);
            state.UserFeedbackUpdates.Add("Game saved successfully");
        }
        catch (Exception)
        {
            state.UserFeedbackUpdates.Add("Unable to save game");
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
