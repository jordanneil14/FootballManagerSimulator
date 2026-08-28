using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Screens;

public class SelectClubScreen(
    IState state,
    IGameCreator gameCreator,
    IGameFactory gameFactory) : IBaseScreen
{
    private readonly IState State = state;
    private readonly IGameCreator GameCreator = gameCreator;
    private readonly IGameFactory GameFactory = gameFactory;

    public ScreenType Screen => ScreenType.SelectClub;

    public void HandleInput(string input)
    {
        switch (input.ToLower())
        {
            case "b":
                State.ScreenStack.Pop();
                break;
            default:
                if (string.IsNullOrWhiteSpace(input)) return;
                var club = GameCreator.Clubs
                    .FirstOrDefault(c => c.Name.ToLower() == input.ToLower() && c.LeagueId == GameCreator.LeagueId);
                if (club == null) return;
                GameCreator.ClubId = club.Id;
                GameFactory.FinaliseGameState();
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
        Console.WriteLine($"{"Team",-30}{"Transfer Budget",-20}{"Stadium",-30}{"Key Player",-25}");
        Console.WriteLine("----------------------------------------------------------------------------------------------------");

        var clubs = GameCreator.Clubs.Where(p => p.LeagueId == GameCreator.LeagueId);

        var clubIds = clubs.Select(p => p.Id);
		var players = State.Players.Where(p => p.Contract != null && clubIds.Contains(p.Contract.ClubId));

		var orderedClubs = clubs.OrderBy(p => p.Name);
        foreach (var club in orderedClubs)
        {
            var transferValueFriendly = $"£{club.TransferBudget:n}";

            var bestPlayer = players.Where(p => p.Contract!.ClubId == club.Id).OrderByDescending(p => p.Rating).FirstOrDefault()?.Name;

            Console.WriteLine($"{club.Name,-30}{transferValueFriendly,-20}{club.Stadium,-30}{bestPlayer,-25}");
        }

        Console.WriteLine("\nOptions:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Club Name>) To Manage Club");
    }
}
