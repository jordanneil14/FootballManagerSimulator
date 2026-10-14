using FootballManagerSimulator.Competitions.Services;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.PlayerGenerator;
using Microsoft.Extensions.Options;

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
    IEventManager eventManager,
    IAppStorage appStorage) : IGameFactory
{
    private readonly IAppStorage AppStorage = appStorage;

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
    private readonly IEventManager EventManager = eventManager;

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

        EventManager.ExecuteEvents();

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

        var generator = new PlayerGenerator.PlayerGenerator();
        var startDate = Settings.General.StartDate;
        var allPlayers = new List<PlayerModel>();
        var random = new Random();

        var leagueRankLookup = Settings.Competitions
            .Where(c => c.IsLeague)
            .ToDictionary(c => c.Id, c => c.Rank);

        foreach (var club in State.Clubs)
        {
            var leagueRank = leagueRankLookup.GetValueOrDefault(club.LeagueId, 1);
            var squadSize = random.Next(25, 31);
            var generated = generator.GenerateSquad(club.Name, squadSize, startDate, leagueRank);
            foreach (var gp in generated)
            {
                allPlayers.Add(MapToPlayerModel(gp, club));
            }
        }

        var freeAgentCount = random.Next(80, 121);
        var freeAgents = generator.GeneratePlayers(freeAgentCount);
        foreach (var gp in freeAgents)
        {
            allPlayers.Add(MapToPlayerModel(gp, null));
        }

        for (int i = 0; i < allPlayers.Count; i++)
            allPlayers[i].Id = i + 1;

        State.Players.AddRange(allPlayers);

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

    private static PlayerModel MapToPlayerModel(GeneratedPlayer gp, ClubModel? club)
    {
        var player = new PlayerModel
        {
            Name = gp.Name,
            Nationality = gp.Nationality,
            NationalKit = gp.NationalKit,
            Position = gp.ClubPosition,
            ShirtNumber = gp.ShirtNumber,
            ClubJoining = gp.ClubJoining,
            Rating = gp.Rating,
            Height = gp.Height,
            Weight = gp.Weight,
            PreferredFoot = gp.PreferredFoot,
            BirthDate = gp.BirthDate,
            Age = gp.Age,
            PreferredPosition = gp.PreferredPosition,
            WorkRate = gp.WorkRate,
            Weakfoot = gp.Weakfoot,
            SkillMoves = gp.SkillMoves,
            BallControl = gp.BallControl,
            Dribbling = gp.Dribbling,
            Marking = gp.Marking,
            SlidingTackle = gp.SlidingTackle,
            StandingTackle = gp.StandingTackle,
            Aggression = gp.Aggression,
            Reactions = gp.Reactions,
            AttackingPosition = gp.AttackingPosition,
            Interceptions = gp.Interceptions,
            Vision = gp.Vision,
            Composure = gp.Composure,
            Crossing = gp.Crossing,
            ShortPass = gp.ShortPass,
            LongPass = gp.LongPass,
            Acceleration = gp.Acceleration,
            Speed = gp.Speed,
            Stamina = gp.Stamina,
            Strength = gp.Strength,
            Balance = gp.Balance,
            Agility = gp.Agility,
            Jumping = gp.Jumping,
            Heading = gp.Heading,
            ShotPower = gp.ShotPower,
            Finishing = gp.Finishing,
            LongShots = gp.LongShots,
            Curve = gp.Curve,
            FreekickAccuracy = gp.FreekickAccuracy,
            Penalties = gp.Penalties,
            Volleys = gp.Volleys,
            GKPositioning = gp.GKPositioning,
            GKDiving = gp.GKDiving,
            GKKicking = gp.GKKicking,
            GKHandling = gp.GKHandling,
            GKReflexes = gp.GKReflexes
        };

        if (club != null && gp.Contract != null)
        {
            player.Contract = new PlayerModel.ContractModel
            {
                ClubId = club.Id,
                ClubName = club.Name,
                ExpiryDate = DateOnly.Parse(gp.Contract.ExpiryDate)
            };
        }

        return player;
    }
}
