using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using System.Globalization;

namespace FootballManagerSimulator.Screens;

public class CreateManagerScreen : IBaseScreen
{
    private readonly IState State;

    public CreateManagerScreen(IState state)
    {
        State = state;
    }

    public ScreenType Screen => ScreenType.CreateManager;

    public void HandleInput(string input)
    {
        var text = new CultureInfo("en-US", false).TextInfo;
        State.ManagerName = text.ToTitleCase(input.ToLower());
        State.ScreenStack.Push(new Structures.Screen
        {
            Type = ScreenType.SelectTeam
        });
    }

    public void RenderScreen()
    {
        Console.WriteLine("Create Manager\n");
        Console.WriteLine("Enter your name: ");
    }
}
