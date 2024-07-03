using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Screens;

public class SelectLeagueScreen(
    IState state,
    IOptions<Settings> settings,
    IGameCreator gameCreator) : IBaseScreen
{
    private readonly Settings Settings = settings.Value;

    public ScreenType Screen => ScreenType.SelectLeague;

    public void HandleInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        var league = gameCreator.Competitions.Where(p => p.Type == CompetitionType.League.ToString()).FirstOrDefault(p => p.Id.ToString() == input);

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
        Console.WriteLine($"{"Id",-10}{"League",-30}{"Country",-20}{"Rank",-10}{"No of Teams",-15}");
        Console.WriteLine("----------------------------------------------------------------------------------");

        var leagues = gameCreator.Competitions.Where(p => p.Type == CompetitionType.League.ToString());

        foreach (var league in leagues)
        {
            var countryName = Settings.Countries.First(p => p.Id == league.CountryId).Name;
            Console.WriteLine($"{league.Id,-10}{league.Name,-30}{countryName,-20}{league.Rank,-10}{league.LeagueTable.Places,-15}");
        }

        Console.WriteLine("\nOptions:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Id>) To Select League");
    }
}
