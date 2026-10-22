using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Exceptions;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.PlayerGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Maui.Helpers;

public class ProcessHelper(
    IState state,
    IWeatherHelper weatherHelper,
    ITransferListHelper transferListHelper,
    IEnumerable<ICompetitionProvider> competitionFactories,
    IEventManager eventManager,
    IOptions<SettingsModel> settings,
    INotificationFactory notificationFactory,
    FriendlyService friendlyService,
    ITacticHelper tacticHelper,
    IServiceProvider serviceProvider) : IProcessHelper
{
    private readonly IState State = state;
    private readonly IWeatherHelper WeatherHelper = weatherHelper;
    private readonly ITransferListHelper TransferListHelper = transferListHelper;
    private readonly IEnumerable<ICompetitionProvider> CompetitionFactories = competitionFactories;
    private readonly IEventManager EventManager = eventManager;
    private readonly SettingsModel Settings = settings.Value;
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    private readonly FriendlyService FriendlyService = friendlyService;
    private readonly ITacticHelper TacticHelper = tacticHelper;
    private readonly IServiceProvider ServiceProvider = serviceProvider;

    // Advances the simulation by one day. If the user's club (or any club) has an
    // unplayed fixture scheduled for the current day, a ProcessException is thrown so
    // the presentation layer can route the user to today's fixtures first. The date is
    // only advanced once there are no outstanding fixtures for the current day.
    public void Process()
    {
        ValidateProcess();
        State.Date = State.Date.AddDays(1);
        State.Weather = WeatherHelper.GetTodaysWeather();

        if (State.Date.Month == 6 && State.Date.Day == 30)
        {
            ProcessEndOfSeason();
            return;
        }

        if (State.Date.DayOfWeek == DayOfWeek.Monday)
            TransferListHelper.UpdateTransferList();

        var m = State.Date.Month;
        var d = State.Date.Day;
        var windowOpen = (m >= 6 && m <= 8 && !(m == 6 && d < 15)) || m == 1;
        if (windowOpen)
            TransferListHelper.ProcessAITransfers();

        foreach (var comp in State.Competitions.Where(p => p.Year == State.Date.Year || p.Year == State.Date.Year - 1))
        {
            var fixture = comp.Fixtures
                .Where(p => p.Date >= State.Date && (p.HomeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id || p.AwayClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id))
                .OrderBy(p => p.Round)
                .FirstOrDefault();

            if (fixture != null && fixture.Date.DayNumber == State.Date.DayNumber + 1)
                CompetitionFactories.First(p => p.Type == comp.Type).CompetitionService.GeneratePreMatchReportForFixture(fixture);
        }

        EventManager.ExecuteEvents();
    }

    private void ProcessEndOfSeason()
    {
        ProcessPromotionAndRelegation();
        ExpireContracts();
        TopUpSquads();
        ResetSuspensions();
        RecreateCompetitions();

        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            "New Season",
            "The new season has begun. Good luck!");
    }

    private void ProcessPromotionAndRelegation()
    {
        var leagues = State.Competitions
            .Where(p => p.IsLeague)
            .Cast<LeagueModel>()
            .ToList();

        var leaguesByCountry = leagues.GroupBy(p => p.CountryId);

        // After a save/load, league and fixture clubs are copies of State.Clubs, so update by Id.
        var clubsById = State.Clubs.ToDictionary(p => p.Id);

        foreach (var countryLeagues in leaguesByCountry)
        {
            var orderedLeagues = countryLeagues.OrderBy(p => p.Rank).ToList();

            for (var i = 0; i < orderedLeagues.Count; i++)
            {
                var league = orderedLeagues[i];
                var table = league.GenerateLeagueTable().ToList();

                if (i > 0)
                {
                    var leagueAbove = orderedLeagues[i - 1];

                    var promoted = table
                        .Take(league.AutomaticPromotionPlaces)
                        .Select(p => p.Club)
                        .ToList();

                    var playoffWinner = league.Fixtures
                        .Where(p => p.FixtureType == FixtureType.PlayoffFinal && p.Concluded)
                        .Select(p => p.ClubWon)
                        .FirstOrDefault();

                    if (playoffWinner != null)
                        promoted.Add(playoffWinner);

                    foreach (var club in promoted)
                        clubsById[club.Id].LeagueId = leagueAbove.Id;
                }

                if (i < orderedLeagues.Count - 1)
                {
                    var leagueBelow = orderedLeagues[i + 1];

                    var relegated = table
                        .TakeLast(league.RelegationPlaces)
                        .Select(p => p.Club)
                        .ToList();

                    foreach (var club in relegated)
                        clubsById[club.Id].LeagueId = leagueBelow.Id;
                }
            }
        }
    }

    private void ExpireContracts()
    {
        var expiredPlayers = State.Players
            .Where(p => p.Contract != null && DateOnly.Parse($"{State.Date.Year}-06-30") >= p.Contract.ExpiryDate)
            .ToList();

        foreach (var player in expiredPlayers)
        {
            player.Contract = null;
            TransferListHelper.RemovePlayerFromTransferList(player.Id);
        }
    }

    private void TopUpSquads()
    {
        var generator = new PlayerGenerator.PlayerGenerator();
        var random = new Random();
        var startDate = $"{State.Date.Year}-07-01";
        var nextId = State.Players.Max(p => p.Id) + 1;

        var leagueRankLookup = Settings.Competitions
            .Where(c => c.IsLeague)
            .ToDictionary(c => c.Id, c => c.Rank);

        foreach (var club in State.Clubs)
        {
            var contractedCount = State.Players
                .Count(p => p.Contract != null && p.Contract.ClubId == club.Id);

            if (contractedCount >= 25)
                continue;

            var targetSize = random.Next(25, 31);
            var playersNeeded = targetSize - contractedCount;

            if (playersNeeded <= 0)
                continue;

            var leagueRank = leagueRankLookup.GetValueOrDefault(club.LeagueId, 1);
            var generated = generator.GenerateSquad(club.Name, playersNeeded, startDate, leagueRank);

            foreach (var gp in generated)
            {
                var player = MapToPlayerModel(gp, club);
                player.Id = nextId++;
                State.Players.Add(player);
            }
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

    private void ResetSuspensions()
    {
        foreach (var player in State.Players.Where(p => p.SuspensionMatches > 0))
            player.SuspensionMatches = 0;
    }

    private void RecreateCompetitions()
    {
        State.Competitions.Clear();
        State.Events.Clear();

        foreach (var competition in Settings.Competitions.OrderByDescending(p => p.Type == "Cup"))
        {
            var competitionProvider = CompetitionFactories
                .First(p => p.Type.ToString() == competition.Type);

            var newCompetition = competitionProvider.CreateCompetition(competition);

            State.Competitions.Add(newCompetition);
        }

        foreach (var league in State.Competitions.Where(p => p.Type == CompetitionType.EnglishFootballLeague).Cast<LeagueModel>())
        {
            if (league.PlayoffPlaces <= 0)
                continue;

            var lastBaseFixtureDate = league.Fixtures
                .Where(p => p.FixtureType == FixtureType.Base)
                .Max(p => p.Date);

            var triggerDate = lastBaseFixtureDate.AddDays(1);

            var playoffEvent = ActivatorUtilities.CreateInstance<EnglishFootballLeaguePlayoffEvent>(
                ServiceProvider, triggerDate, false, league.Id);
            EventManager.ValidateAndAddEvent(playoffEvent);
        }

        foreach (var league in State.Competitions.Where(p => p.Type == CompetitionType.EnglishNationalLeague).Cast<LeagueModel>())
        {
            if (league.PlayoffPlaces < 6)
                continue;

            var lastBaseFixtureDate = league.Fixtures
                .Where(p => p.FixtureType == FixtureType.Base)
                .Max(p => p.Date);

            var triggerDate = lastBaseFixtureDate.AddDays(1);

            var playoffEvent = ActivatorUtilities.CreateInstance<NationalLeaguePlayoffQuarterFinalEvent>(
                ServiceProvider, triggerDate, false, league.Id);
            EventManager.ValidateAndAddEvent(playoffEvent);
        }

        foreach (var competition in State.Competitions.Where(p => p.Type == CompetitionType.Friendly))
        {
            foreach (var drawDate in competition.DrawSettings)
                FriendlyService.GenerateNextRoundOfFixtures(competition);
        }

        foreach (var club in State.Clubs)
            TacticHelper.ResetTacticForClub(club);
    }

    private void ValidateProcess()
    {
        var existsOutstandingFixtures = State.Competitions
            .SelectMany(p => p.Fixtures)
            .Any(p => !p.Concluded && p.Date == State.Date);

        if (existsOutstandingFixtures)
            throw new ProcessException(ScreenType.Fixture);
    }
}
