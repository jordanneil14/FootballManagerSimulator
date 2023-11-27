using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Factories;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class WelcomeScreen : IBaseScreen
{
    private readonly IState State;
    private readonly IUtils Utils;
    private readonly ICompetitionFactory LeagueFactory;

    public WelcomeScreen(
        IState state, 
        IUtils utils,
        ICompetitionFactory leagueFactory)
    {
        State = state;
        Utils = utils;
        LeagueFactory = leagueFactory;
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
        State.Clubs = Utils.GetResource<IEnumerable<Club>>("teams.json"); 
        
        var playerItems = Utils.GetResource<IEnumerable<Player.SerialisablePlayerModel>>("playerData.json");
        Utils.MapPlayersToAClub(playerItems.ToList());

        var competitions = Utils.GetResource<IEnumerable<Competition>>("competitions.json");
        foreach(var competition in competitions)
        {
            var league = LeagueFactory.CreateCompetition(competition);
            State.Competitions.Add(league);
        }
    }

    public void RenderScreen()
    {
        Console.WriteLine("Welcome to Football Manager Simulator\n");
        Console.WriteLine("Options:");
        Console.WriteLine("A) Start New Game");
        Console.WriteLine("B) Load Game");
        Console.WriteLine("Q) Quit");
    }
}
