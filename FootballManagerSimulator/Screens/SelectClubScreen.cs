using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class SelectClubScreen : IBaseScreen
{
    private readonly IState State;
    private readonly IGameCreator GameCreator;
    private readonly IGameFactory GameFactory;

    public SelectClubScreen(
        IState state, 
        IGameCreator gameCreator,
        IGameFactory gameFactory)
    {
        State = state;
        GameCreator = gameCreator;
        GameFactory = gameFactory;
    }

    public ScreenType Screen => ScreenType.SelectClub;

    public void HandleInput(string input)
    {
        switch(input.ToLower())
        {
            case "b":
                State.ScreenStack.Pop();
                break;
            default:
                if (string.IsNullOrWhiteSpace(input)) return;
                var club = GameCreator.Clubs.FirstOrDefault(c => c.Name.ToLower() == input.ToLower() && c.LeagueId == GameCreator.LeagueId);
                if (club == null) return;
                GameCreator.ClubId = club.Id;
                GameFactory.CreateGame();
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.Main,
                });
                break;
        }
    }

    public void RenderScreen()
    {
        Console.WriteLine("Select a club to manage:\n");
        Console.WriteLine($"{"Team",-50}");

        var clubs = GameCreator.Clubs.Where(p => p.LeagueId == GameCreator.LeagueId);
        var orderedClubs = clubs.OrderBy(p => p.Name);
        foreach (var club in orderedClubs)
        {
            Console.WriteLine($"{club.Name,-50}");
        }

        Console.WriteLine("\nOptions:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Club Name>) To Manage Club");
    }
}
