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
    IJobOfferHelper jobOfferHelper,
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
    private readonly IJobOfferHelper JobOfferHelper = jobOfferHelper;
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

        // Checked the day after a result so the match day itself plays out normally.
        if (State.MyClubId != null && State.BoardConfidence < BoardConfidenceRules.UserSackedBelow)
            JobOfferHelper.SackUser();

        AnnounceTransferWindowChange();

        if (State.Date.Month == 6 && State.Date.Day == 30)
        {
            ProcessEndOfSeason();
            return;
        }

        if (State.Date.Month == 3 && State.Date.Day == 1)
            ProcessYouthIntake();

        JobOfferHelper.ProcessVacancies();

        if (State.Date.DayOfWeek == DayOfWeek.Monday)
        {
            TransferListHelper.UpdateTransferList();
            JobOfferHelper.ProcessJobOffers();
        }

        if (TransferWindowRules.IsOpen(State.Date))
            TransferListHelper.ProcessAITransfers();

        foreach (var comp in State.Competitions.Where(p => p.Year == State.Date.Year || p.Year == State.Date.Year - 1))
        {
            var fixture = comp.Fixtures
                .Where(p => p.Date >= State.Date && (p.HomeClub.Id == State.MyClubId || p.AwayClub.Id == State.MyClubId))
                .OrderBy(p => p.Round)
                .FirstOrDefault();

            if (fixture != null && fixture.Date.DayNumber == State.Date.DayNumber + 1)
                CompetitionFactories.First(p => p.Type == comp.Type).CompetitionService.GeneratePreMatchReportForFixture(fixture);
        }

        EventManager.ExecuteEvents();
    }

    // The window dates are fixed, so compare today with yesterday rather than scheduling an event.
    private void AnnounceTransferWindowChange()
    {
        var isOpen = TransferWindowRules.IsOpen(State.Date);
        if (isOpen == TransferWindowRules.IsOpen(State.Date.AddDays(-1)))
            return;

        if (isOpen)
            NotificationFactory.AddNotification(
                State.Date,
                "Chairman",
                "Transfer Window Open",
                $"The transfer window is now open and closes on {TransferWindowRules.ClosesOn(State.Date):d MMMM yyyy}.");
        else
            NotificationFactory.AddNotification(
                State.Date,
                "Chairman",
                "Transfer Window Closed",
                $"The transfer window has closed. It reopens on {TransferWindowRules.NextOpening(State.Date):d MMMM yyyy}.");
    }

    private void ProcessEndOfSeason()
    {
        RecordLeagueTitle();
        ProcessPromotionAndRelegation();
        ExpireContracts();
        TopUpSquads();
        ResetSuspensions();
        RecreateCompetitions();
        var myIncrease = IncreaseTransferBudgets();

        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            "New Season",
            $"The new season has begun. The board has added £{myIncrease:n0} to your transfer budget. Good luck!");
    }

    // Each club receives its seasonal allocation (its starting budget from settings) on
    // top of whatever it has left. Returns the amount given to the user's club.
    private int IncreaseTransferBudgets()
    {
        var allocations = Settings.Clubs.ToDictionary(p => p.Id, p => p.TransferBudget);
        var myIncrease = 0;

        foreach (var club in State.Clubs)
        {
            var increase = allocations.GetValueOrDefault(club.Id);
            club.TransferBudget = (int)Math.Min((long)club.TransferBudget + increase, int.MaxValue);

            if (club.Id == State.MyClubId)
                myIncrease = increase;
        }

        return myIncrease;
    }

    // Must run before promotion and relegation move the club into next season's league.
    private void RecordLeagueTitle()
    {
        var myClub = State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId);
        var myLeague = State.Competitions.OfType<LeagueModel>().FirstOrDefault(p => p.Id == myClub?.LeagueId);
        if (myClub == null || myLeague == null)
            return;

        if (myLeague.GenerateLeagueTable().FirstOrDefault()?.Club.Id == myClub.Id)
            State.ManagerHistory.AddTrophy(myLeague.Name, State.Date);
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

                if (i > 0)
                {
                    var leagueAbove = orderedLeagues[i - 1];

                    foreach (var club in league.PromotedClubs())
                        clubsById[club.Id].LeagueId = leagueAbove.Id;
                }

                if (i < orderedLeagues.Count - 1)
                {
                    var leagueBelow = orderedLeagues[i + 1];

                    foreach (var club in league.RelegatedClubs())
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

        var myRetirees = expiredPlayers
            .Where(p => p.Contract!.ClubId == State.MyClubId && p.IsRetiring(State.Date))
            .Select(p => p.Name)
            .ToList();

        foreach (var player in expiredPlayers)
        {
            player.Contract = null;
            TransferListHelper.RemovePlayerFromTransferList(player.Id);
        }

        RetirePlayers();

        if (myRetirees.Count > 0)
            NotificationFactory.AddNotification(
                State.Date,
                "Press Officer",
                "Retirements",
                $"The following players have retired following the expiry of their contracts:\n\t{string.Join("\n\t", myRetirees)}");
    }

    // Out-of-contract players at or past the retirement age won't sign again, so they leave the game.
    private void RetirePlayers()
    {
        var retiredIds = State.Players
            .Where(p => p.Contract == null && p.IsRetiring(State.Date))
            .Select(p => p.Id)
            .ToHashSet();

        if (retiredIds.Count == 0)
            return;

        foreach (var slot in State.Clubs.SelectMany(p => p.TacticSlots))
        {
            if (slot.PlayerId is { } playerId && retiredIds.Contains(playerId))
                slot.PlayerId = null;
        }

        State.TransferListItems.RemoveAll(p => retiredIds.Contains(p.PlayerId));
        State.Players.RemoveAll(p => retiredIds.Contains(p.Id));
    }

    // Every club promotes a handful of 16-17 year olds from its academy on 1 March.
    private void ProcessYouthIntake()
    {
        var generator = new PlayerGenerator.PlayerGenerator();
        var random = new Random();
        var startDate = State.Date.ToString("yyyy-MM-dd");
        var nextId = State.Players.Max(p => p.Id) + 1;
        var myIntake = new List<PlayerModel>();

        var leagueRankLookup = Settings.Competitions
            .Where(c => c.IsLeague)
            .ToDictionary(c => c.Id, c => c.Rank);

        foreach (var club in State.Clubs)
        {
            var leagueRank = leagueRankLookup.GetValueOrDefault(club.LeagueId, 1);
            var generated = generator.GenerateYouthIntake(club.Name, random.Next(2, 5), startDate, leagueRank, club.AcademyRating);

            foreach (var gp in generated)
            {
                var player = MapToPlayerModel(gp, club);
                player.Id = nextId++;
                player.ShirtNumber = ShirtNumberRules.NextAvailable(State.Players, club.Id, player.Position);
                State.Players.Add(player);

                var reserveSlot = club.TacticSlots.FirstOrDefault(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId == null);
                if (reserveSlot != null)
                    reserveSlot.PlayerId = player.Id;

                if (club.Id == State.MyClubId)
                    myIntake.Add(player);
            }
        }

        if (myIntake.Count > 0)
            NotificationFactory.AddNotification(
                State.Date,
                "Head of Youth Development",
                "Youth Intake",
                $"The following players have been promoted from the academy:\n\t{string.Join("\n\t", myIntake.Select(p => $"{p.Name} ({p.PreferredPosition}, {p.Rating})"))}");
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
            var contractedPositions = State.Players
                .Where(p => p.Contract != null && p.Contract.ClubId == club.Id)
                .Select(p => p.Position)
                .ToList();

            if (contractedPositions.Count >= 25)
                continue;

            var targetSize = random.Next(25, 31);
            var playersNeeded = targetSize - contractedPositions.Count;

            if (playersNeeded <= 0)
                continue;

            var leagueRank = leagueRankLookup.GetValueOrDefault(club.LeagueId, 1);
            var generated = generator.GenerateTopUp(club.Name, playersNeeded, contractedPositions, startDate, leagueRank);

            // Generated numbers only avoid each other, so number the newcomers around the existing squad, best first.
            foreach (var gp in generated.OrderByDescending(p => p.Rating))
            {
                var player = MapToPlayerModel(gp, club);
                player.Id = nextId++;
                player.ShirtNumber = ShirtNumberRules.NextAvailable(State.Players, club.Id, player.Position);
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

    // Last season's top-flight champions face the cup winners; a double winner faces the league runners-up.
    private List<int>? ShieldQualifierIds()
    {
        var shield = Settings.Competitions.FirstOrDefault(p => p.Type == CompetitionType.EnglishCommunityShield.ToString());
        if (shield == null)
            return null;

        var leagueId = Settings.Competitions.FirstOrDefault(p => p.IsLeague && p.CountryId == shield.CountryId && p.Rank == 1)?.Id;
        var cupId = Settings.Competitions.FirstOrDefault(p => p.Type == CompetitionType.Cup.ToString() && p.CountryId == shield.CountryId)?.Id;

        var league = State.Competitions.OfType<LeagueModel>().FirstOrDefault(p => p.Id == leagueId);
        var table = league?.GenerateLeagueTable().Select(p => p.Club.Id).ToList();
        if (table == null || table.Count < 2)
            return null;

        var cup = State.Competitions.FirstOrDefault(p => p.Id == cupId);
        var finalRound = cup?.DrawSettings.Count > 0 ? cup.DrawSettings.Max(p => p.Round) : (int?)null;
        var cupWinnerId = cup?.Fixtures
            .Where(p => p.Round == finalRound && p.Concluded && p.ClubWon != null)
            .Select(p => (int?)p.ClubWon!.Id)
            .FirstOrDefault();

        var champions = table[0];
        var opponent = cupWinnerId is { } winner && winner != champions ? winner : table[1];
        return [champions, opponent];
    }

    private void ApplyShieldQualifiers(List<int> clubIds)
    {
        var shield = State.Competitions.FirstOrDefault(p => p.Type == CompetitionType.EnglishCommunityShield);
        if (shield == null)
            return;

        shield.Clubs = clubIds.Select(id => State.Clubs.First(p => p.Id == id)).ToList();
        shield.DrawSettings.First().IntroducedClubIds = clubIds;
    }

    private void RecreateCompetitions()
    {
        var shieldClubIds = ShieldQualifierIds();
        State.Competitions.Clear();

        // Only last season's competition events go; club projects already paid for (and past
        // requests, which set the cooldown on asking again) carry over into the new season.
        State.Events.RemoveAll(p => p is not (IncreaseTransferBudgetRequestEvent
            or StadiumExpansionEvent or AcademyUpgradeEvent));

        foreach (var competition in Settings.Competitions.OrderByDescending(p => p.Type == "Cup"))
        {
            var competitionProvider = CompetitionFactories
                .First(p => p.Type.ToString() == competition.Type);

            var newCompetition = competitionProvider.CreateCompetition(competition);

            State.Competitions.Add(newCompetition);
        }

        if (shieldClubIds != null)
            ApplyShieldQualifiers(shieldClubIds);

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

        CompetitionEndEvent.ScheduleForCompetitions(State, EventManager, ServiceProvider);

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
