using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.PlayerGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Maui.Factories;

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
    IAppStorage appStorage,
    IScoutHelper scoutHelper,
    IServiceProvider serviceProvider) : IGameFactory
{
    private readonly IScoutHelper ScoutHelper = scoutHelper;
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
    private readonly IServiceProvider ServiceProvider = serviceProvider;

    public void FinaliseGameState()
    {
		State.ManagerName = GameCreator.ManagerName;
		State.MyClubId = GameCreator.ClubId;

		State.ManagerHistory.StartJob(State.Clubs.First(p => p.Id == State.MyClubId), State.Date);

		CreateStartingVacancies();
		SeedAiJobSecurity();

		State.Weather = WeatherHelper.GetTodaysWeather();

		NotificationFactory.AddNotification(
			State.Date,
			"Chairman",
			$"Welcome to {State.Clubs.First(p => p.Id == State.MyClubId).Name}",
			"Everyone at the club wishes you a successful reign as manager");

		NotificationFactory.AddNotification(
			State.Date,
			"Chairman",
			"Budget",
			$"Your budget for the upcoming season is {State.Clubs.First(p => p.Id == State.MyClubId).TransferBudgetFriendly}");

		var freeAgents = State.Players
			.Where(p => p.Contract == null)
			.OrderByDescending(p => p.Rating)
			.Take(4)
			.ToList();

		// The free agents named below must be on the Scout page too.
		ScoutHelper.SeedDiscoveredPlayers();
		State.DiscoveredPlayerIds.AddRange(freeAgents.Select(p => p.Id).Except(State.DiscoveredPlayerIds).ToList());

		NotificationFactory.AddNotification(
			State.Date.AddDays(1),
			"Scout",
			"Players With Expired Contracts",
			$"Congratulations on your new job! There are lots of free agents on the marketplace at the minute. Here are small list of players which you may be interested in:\n\t{string.Join("\n\t", freeAgents.Select(p => p.Name))}{Environment.NewLine}Free agents can be found on the Scout page, where you can also hire scouts to find more players");

		foreach (var competition in State.Competitions.Where(p => p.Type == Enums.CompetitionType.Friendly))
		{
			foreach (var drawDate in competition.DrawSettings)
                FriendlyService.GenerateNextRoundOfFixtures(competition);
        }

        EventManager.ExecuteEvents();

		TransferListHelper.UpdateTransferList();
	}

    private void CreateStartingVacancies()
    {
        var count = RandomNumberHelper.Next(BoardConfidenceRules.StartingVacanciesMin, BoardConfidenceRules.StartingVacanciesMax + 1);
        var clubs = State.Clubs
            .Where(p => p.Id != State.MyClubId)
            .OrderBy(_ => RandomNumberHelper.Next())
            .Take(count);

        foreach (var club in clubs)
            club.VacantUntil = State.Date.AddDays(
                RandomNumberHelper.Next(BoardConfidenceRules.VacancyMinDays, BoardConfidenceRules.VacancyMaxDays + 1));
    }

    private void SeedAiJobSecurity()
    {
        var aiClubs = State.Clubs
            .Where(p => p.Id != State.MyClubId && p.VacantUntil == null)
            .ToList();

        foreach (var club in aiClubs)
            club.JobSecurity = RandomNumberHelper.Next(BoardConfidenceRules.StartingAiSecurityMin, BoardConfidenceRules.StartingAiSecurityMax + 1);

        var count = RandomNumberHelper.Next(BoardConfidenceRules.StartingUnderThreatMin, BoardConfidenceRules.StartingUnderThreatMax + 1);
        foreach (var club in aiClubs.OrderBy(_ => RandomNumberHelper.Next()).Take(count))
            club.JobSecurity = RandomNumberHelper.Next(BoardConfidenceRules.SackedBelow, BoardConfidenceRules.UnderPressureFrom);
    }

    public void IntitialiseGameState()
    {
		State.Date = Settings.General.StartDateAsDate;

		State.Clubs = Settings.Clubs.Select(NewClub).ToList();

        var generator = new PlayerGenerator.PlayerGenerator();
        var startDate = Settings.General.StartDate;
        var allPlayers = StockClubs(State.Clubs, generator, startDate);

        var freeAgentCount = RandomNumberHelper.Next(80, 121);
        var freeAgents = generator.GeneratePlayers(freeAgentCount, startDate);
        foreach (var gp in freeAgents)
        {
            allPlayers.Add(MapToPlayerModel(gp, null));
        }

        for (int i = 0; i < allPlayers.Count; i++)
            allPlayers[i].Id = i + 1;

        State.Players.AddRange(allPlayers);

        foreach (var club in State.Clubs)
            ResetTactics(club);

        foreach (var competition in Settings.Competitions.OrderBy(p => p.CreationOrder()))
        {
            var competitionProvider = CompetitionProviders
                .First(p => p.Type.ToString() == competition.Type);

            var competition1 = competitionProvider.CreateCompetition(competition);

            State.Competitions.Add(competition1);
        }

        PlayoffScheduler.Schedule(State, EventManager, ServiceProvider);
        CompetitionEndEvent.ScheduleForCompetitions(State, EventManager, ServiceProvider);
    }

    private static ClubModel NewClub(ClubModel settingsClub) => new()
    {
        Id = settingsClub.Id,
        Name = settingsClub.Name,
        Stadium = settingsClub.Stadium,
        StadiumSize = settingsClub.StadiumSize,
        WageBudget = settingsClub.WageBudget,
        LeagueId = settingsClub.LeagueId,
        Colour = settingsClub.Colour
    };

    // Gives each club its budget, academy rating and a squad rated for its division; returns the squads' players.
    private List<PlayerModel> StockClubs(List<ClubModel> clubs, PlayerGenerator.PlayerGenerator generator, string startDate)
    {
        // Same rule as the board's end-of-season allocation, so a new club starts with one season's budget.
        var budgets = SeasonalBudgetRules.Budgets(State.Clubs, Settings.Competitions);

        var leagueRankLookup = Settings.Competitions
            .Where(c => c.IsLeague)
            .ToDictionary(c => c.Id, c => c.Rank);

        var players = new List<PlayerModel>();
        foreach (var club in clubs)
        {
            club.TransferBudget = budgets.GetValueOrDefault(club.Id);
            var leagueRank = leagueRankLookup.GetValueOrDefault(club.LeagueId, 1);
            club.AcademyRating = AcademyRules.StartingRating(leagueRank);
            var squadSize = RandomNumberHelper.Next(25, 31);
            players.AddRange(generator.GenerateSquad(club.Name, squadSize, startDate, leagueRank).Select(gp => MapToPlayerModel(gp, club)));
        }
        return players;
    }

    private void ResetTactics(ClubModel club)
    {
        club.Formation = Helpers.TacticHelper.Formations[RandomNumberHelper.Next(Helpers.TacticHelper.Formations.Length)];
        TacticHelper.ResetTacticForClub(club);
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
