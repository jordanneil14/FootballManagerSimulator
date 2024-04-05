using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using System.Globalization;

namespace FootballManagerSimulator.Screens;

public class CreateManagerScreen : IBaseScreen
{
    private readonly IState State;
    private readonly IGameCreator GameCreator;

    public CreateManagerScreen(
        IState state,
        IGameCreator gameCreator)
    {
        State = state;
        GameCreator = gameCreator;
    }

    public ScreenType Screen => ScreenType.CreateManager;

    public void HandleInput(string input)
    {
        switch(input.ToLower())
        {
            case "b":
                State.ScreenStack.Pop();
                break;
            default:
                if (string.IsNullOrWhiteSpace(input)) return;
                var text = new CultureInfo("en-US", false).TextInfo;
                GameCreator.ManagerName = text.ToTitleCase(input.ToLower());
                State.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.SelectLeague
                });
                break;
        }
        
    }

    public void RenderScreen()
    {
        Console.WriteLine("Create Manager");
        Console.WriteLine("\nOptions:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Your Name>) To Select Manager Name");
    }
}
