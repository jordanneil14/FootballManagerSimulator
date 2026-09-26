using FootballManagerSimulator.Competitions.Services;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace FootballManagerSimulator.Factories;

public class GameFactory(
    IPlayerHelper playerHelper,
    IState state,
    IEnumerable<ICompetitionProvider> competitionFactories,
    INotificationFactory notificationFactory,
    IGameCreator gameCreator,
    IOptions<SettingsModel> settings,
    ITacticHelper tacticHelper,
    IWeatherHelper weatherHelper,
    ITransferListHelper transferListHelper,
    FriendlyService friendlyService,
    IEventManager gameEventManager) : IGameFactory
{
    private readonly SettingsModel Settings = settings.Value;
    private readonly IPlayerHelper PlayerHelper = playerHelper;
    private readonly IState State = state;
    private readonly IEnumerable<ICompetitionProvider> CompetitionProviders = competitionFactories;
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    private readonly IGameCreator GameCreator = gameCreator;
    private readonly ITacticHelper TacticHelper = tacticHelper;
    private readonly IWeatherHelper WeatherHelper = weatherHelper;
    private readonly ITransferListHelper TransferListHelper = transferListHelper;
    private readonly FriendlyService FriendlyService = friendlyService;
    private readonly IEventManager GameEventManager = gameEventManager;

    public void FinaliseGameState()
    {
		State.ManagerName = GameCreator.ManagerName;
		State.MyClubId = GameCreator.ClubId;

		State.Weather = WeatherHelper.GetTodaysWeather();

		NotificationFactory.AddNotification(
			State.Date,
			"Chairman",
			$"Welcome to {State.Clubs.First(p => p.Id == State.MyClubId).Name}",
			"Everyone at the club wishes you a successful reign as manager.");

		NotificationFactory.AddNotification(
			State.Date,
			"Chairman",
			"Transfer Budget",
			$"Your transfer budget for the upcoming season is {State.Clubs.First(p => p.Id == State.MyClubId).TransferBudgetFriendly}.");

		var freeAgents = State.Players
			.Where(p => p.Contract == null)
			.OrderByDescending(p => p.Rating)
			.Select(p => p.Name)
			.Take(4);

		NotificationFactory.AddNotification(
			State.Date.AddDays(1),
			"Scout",
			"Players With Expired Contracts",
			$"Congratulations on your new job! There are lots of free agents on the marketplace at the minute. Here are a\n" +
			$"small list of players which you may be interested in:\n\t{string.Join("\n\t", freeAgents)}{Environment.NewLine}Free agents can be found on the Scout page.");

		foreach (var competition in State.Competitions.Where(p => p.Type == Enums.CompetitionType.Friendly))
		{
			foreach (var drawDate in competition.DrawSettings)
                FriendlyService.GenerateNextRoundOfFixtures(competition);
        }

        GameEventManager.ExecuteEvents();

		TransferListHelper.UpdateTransferList();
	}

    public void IntitialiseGameState()
    {
		State.Date = Settings.General.StartDateAsDate;

		State.Clubs = Settings.Clubs.Select(p => new ClubModel
        {
            Id = p.Id,
            Name = p.Name,
            Stadium = p.Stadium,
            TransferBudget = p.TransferBudget,
            WageBudget = p.WageBudget,
            LeagueId = p.LeagueId
        }).ToList();

        var content = File.ReadAllText($"Resources\\playerData.json");
        var playerData = JsonConvert.DeserializeObject<PlayerData>(content);
        if (playerData == null)
            throw new Exception("Unable to load players from playerData.json");

        PlayerHelper.AddPlayersToState(playerData);

        foreach (var club in State.Clubs)
            TacticHelper.ResetTacticForClub(club);

        foreach (var competition in Settings.Competitions.OrderByDescending(p => p.Type == "Cup"))
        {
            var competitionProvider = CompetitionProviders
                .First(p => p.Type.ToString() == competition.Type);

            var competition1 = competitionProvider.CreateCompetition(competition);

            State.Competitions.Add(competition1);
        }
    }
}
