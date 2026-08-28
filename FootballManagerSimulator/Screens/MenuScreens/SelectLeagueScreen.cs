using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Screens.MenuScreens;

public class SelectLeagueScreen(
    IState state,
    IOptions<Settings> settings,
    IGameCreator gameCreator) : MenuBaseScreen
{
    private readonly Settings Settings = settings.Value;
    private readonly IState State = state;
    private readonly IGameCreator GameCreator = gameCreator;

    public override ScreenType Screen => ScreenType.SelectLeague;

	public override Dictionary<string, string> Options => new() {
	};

	public override string? OptionPrompt => "Enter a league Id: ";

	public override void HandleInput(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return;

		var league = GameCreator.Competitions.Where(p => p.Type == CompetitionType.League.ToString()).FirstOrDefault(p => p.Id.ToString() == input);
		if (league != null)
		{
			GameCreator.LeagueId = league.Id;

			State.ScreenStack.Push(new Screen
			{
				Type = ScreenType.SelectClub
			});
			return;
		}

		switch (input)
		{
			case "B":
				State.ScreenStack.Pop();
				break;
		}
	}

	public override void RenderSubscreen()
	{
		

		Console.WriteLine($"{"Id",-10}{"League",-30}{"Country",-20}{"Rank",-10}{"No of Teams",-15}");
		Console.WriteLine("----------------------------------------------------------------------------------");

		var leagues = GameCreator.Competitions.Where(p => p.Type == CompetitionType.League.ToString());

		foreach (var league in leagues)
		{
			var countryName = Settings.Countries.First(p => p.Id == league.CountryId).Name;
			Console.WriteLine($"{league.Id,-10}{league.Name,-30}{countryName,-20}{league.Rank,-10}{league.LeagueTable.Places,-15}");
		}
	}

	public override void RenderTop()
	{
		Console.WriteLine("Select a league to manage in");
	}
}
