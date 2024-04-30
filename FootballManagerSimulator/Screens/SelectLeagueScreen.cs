using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class SelectLeagueScreen(
    IState state,
    IGameCreator gameCreator) : IBaseScreen
{
    public ScreenType Screen => ScreenType.SelectLeague;

    public void HandleInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        var league = gameCreator.Leagues.FirstOrDefault(p => p.Id.ToString() == input);
        if (league != null)
        {
            gameCreator.LeagueId = league.Id;

            state.ScreenStack.Push(new Screen
            {
                Type = ScreenType.SelectClub
            });
            return;
        }
        switch (input)
        {
            case "B":
                state.ScreenStack.Pop();
                break;
        }
    }

    public void RenderScreen()
    {
        Console.WriteLine("Select a league to manage in:\n");
        Console.WriteLine($"{"Id",-10}{"League",-40}");

        foreach (var league in gameCreator.Leagues)
        {
            Console.WriteLine($"{league.Id, -10}{league.Name, -40}");
        }

        Console.WriteLine("\nOptions:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter League Id>) To Select League");
    }
}
