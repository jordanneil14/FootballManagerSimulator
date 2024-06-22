using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class SelectClubScreen(
    IState state,
    IGameCreator gameCreator,
    IGameFactory gameFactory) : IBaseScreen
{
    public ScreenType Screen => ScreenType.SelectClub;

    public void HandleInput(string input)
    {
        switch(input.ToLower())
        {
            case "b":
                state.ScreenStack.Pop();
                break;
            default:
                if (string.IsNullOrWhiteSpace(input)) return;
                var club = gameCreator.Clubs
                    .FirstOrDefault(c => c.Name.ToLower() == input.ToLower() && c.LeagueId == gameCreator.LeagueId);
                if (club == null) return;
                gameCreator.ClubId = club.Id;
                gameFactory.CreateGame();
                state.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.Main,
                });
                break;
        }
    }

    public void RenderScreen()
    {
        Console.WriteLine("Select a club to manage:\n");
        Console.WriteLine($"{"Team",-30}{"Transfer Budget", -20}{"Stadium", -20}");

        var clubs = gameCreator.Clubs.Where(p => p.LeagueId == gameCreator.LeagueId);
        var orderedClubs = clubs.OrderBy(p => p.Name);
        foreach (var club in orderedClubs)
        {
            var transferValueFriendly = $"£{club.TransferBudget:n}";
            Console.WriteLine($"{club.Name,-30}{transferValueFriendly, -20}{club.Stadium, -20}");
        }

        Console.WriteLine("\nOptions:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Club Name>) To Manage Club");
    }
}
