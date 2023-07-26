using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class WelcomeScreen : IBaseScreen
{
    private readonly IState State;
    private readonly IHelperFunction HelperFunction;

    public WelcomeScreen(
        IState state, 
        IHelperFunction helperFunction)
    {
        State = state;
        HelperFunction = helperFunction;
    }

    public ScreenType Screen => ScreenType.Welcome;

    public void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                SetupStateForNewGame();
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.CreateManager
                });
                break;
            case "B":
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.LoadGame
                });
                break;
            case "Q":
                Environment.Exit(0);
                break;
            default:
                break;
        }
    }

    public void SetupStateForNewGame()
    {
        State.Teams = HelperFunction.GetTeams();
        var playerItems = HelperFunction.GetPlayers().ToList();
        AssignPlayersToTeams(playerItems);
    }

    private void AssignPlayersToTeams(List<Player.SerialisablePlayerModel> players)
    {
        foreach (var team in State.Teams)
        {
            for (int i = 0; i < 25; i++)
            {
                var player = Player.FromPlayerItem(players.ElementAt(i), i, team);
                State.Players.Add(player);
            }
            players.RemoveRange(0, 25);
        }

        //Free agents
        for (int i = 0; i < players.Count; i++)
        {
            var player = Player.FromPlayerItem(players.ElementAt(i), null, null);
            State.Players.Add(player);
        }
    }

    public void RenderScreen()
    {
        Console.WriteLine("Welcome to Football Manager Simulator\n");
        Console.WriteLine("A) Start New Game");
        Console.WriteLine("B) Load Game");
        Console.WriteLine("Q) Quit");
    }
}
