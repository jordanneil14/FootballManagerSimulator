using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Exceptions;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;
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
    IScoutHelper scoutHelper,
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
    private readonly IScoutHelper ScoutHelper = scoutHelper;
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
        ScoutHelper.ProcessScouts();
        ProcessPhysios();

        if (State.Date.Month == 6 && State.Date.Day == 30)
        {
            ProcessEndOfSeason();
            return;
        }

        if (State.Date.Month == 3 && State.Date.Day == 1)
            ProcessYouthIntake();

        // Six and three months before contracts run out at the end of the season.
        if (State.Date.Day == 30 && (State.Date.Month == 12 || State.Date.Month == 3))
            AnnounceExpiringContracts();

        if (State.Date.Month == ContractRenewalRules.RenewalMonth && State.Date.Day == ContractRenewalRules.RenewalDay)
            RenewAIContracts();

        JobOfferHelper.ProcessVacancies();

        if (State.Date.DayOfWeek == DayOfWeek.Monday)
        {
            TransferListHelper.UpdateTransferList();
            JobOfferHelper.ProcessJobOffers();
        }

        // Free agents can sign at any time; only deals between clubs wait for the window.
        TransferListHelper.ProcessAIFreeAgentSignings();
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

    // AI clubs re-sign most of the players whose contracts end this season; the rest leave
    // as free agents on 30 June. The user's club is left alone, since renewals are their call.
    private void RenewAIContracts()
    {
        var seasonEnd = new DateOnly(State.Date.Year, 6, 30);
        var squads = State.Players
            .Where(p => p.Contract != null && p.Contract.ClubId != State.MyClubId)
            .GroupBy(p => p.Contract!.ClubId);

        foreach (var squad in squads)
        {
            // Ranked across the whole squad, so the first team means the same at every club.
            var ranked = squad.OrderByDescending(p => p.Rating).Select((player, index) => (player, rank: index + 1)).ToList();
            var strength = ranked.Take(GameEngine.SquadAverageBestPlayers).Average(p => p.player.Rating);
            foreach (var (player, rank) in ranked.Where(p => p.player.Contract!.ExpiryDate <= seasonEnd))
            {
                if (RandomNumberHelper.NextDouble() >= ContractRenewalRules.RenewalChance(player, rank, strength, seasonEnd))
                    continue;

                var years = ContractRenewalRules.RenewalYears(player.GetAge(seasonEnd));
                player.Contract!.ExpiryDate = seasonEnd.AddYears(years);
            }
        }
    }

    private void AnnounceExpiringContracts()
    {
        if (State.MyClubId == null)
            return;

        var seasonEnd = new DateOnly(State.Date.Month > 6 ? State.Date.Year + 1 : State.Date.Year, 6, 30);
        var expiring = State.Players
            .Where(p => p.Contract?.ClubId == State.MyClubId && p.Contract.ExpiryDate <= seasonEnd)
            .OrderByDescending(p => p.Rating)
            .ToList();

        if (expiring.Count == 0)
            return;

        var months = State.Date.Month == 12 ? 6 : 3;
        var lines = expiring.Select(p =>
            $"{p.Name} ({p.PreferredPosition}, {p.Rating}){(p.IsRetiring(seasonEnd) ? " - retiring" : "")}");

        NotificationFactory.AddNotification(
            State.Date,
            "Assistant Manager",
            "Expiring Contracts",
            $"These players' contracts expire in {months} months, on {seasonEnd:d MMMM yyyy}. Offer new contracts from their player pages or they will leave the club. Players who are retiring will not sign again:\n\t{string.Join("\n\t", lines)}");
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
        TransferListHelper.SignFreeAgentsForShortSquads();
        TopUpSquads(SquadSizeRules.Minimum);
        ResetSuspensions();
        RecreateCompetitions();
        var (myIncrease, myReturned) = IncreaseTransferBudgets();

        var returned = myReturned > 0 ? $" £{myReturned:n0} left unspent last season has gone back to the board." : "";
        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            "New Season",
            $"The new season has begun. The board has added £{myIncrease:n0} to your budget.{returned} Good luck!");
    }

    // Each club receives its seasonal allocation for the league it is now in (see SeasonalBudgetRules)
    // on top of whatever it has left, then is capped: AI clubs at 1.5x that allocation, the user's at 1x.
    // Returns the amount given to the user's club and how much of its leftover the board took back.
    // Must run after promotion and relegation so clubs are paid, and capped, for their new division.
    private (int Increase, int Returned) IncreaseTransferBudgets()
    {
        var allocations = SeasonalBudgetRules.Budgets(State.Clubs, Settings.Competitions);
        var (myIncrease, myReturned) = (0, 0);

        foreach (var club in State.Clubs)
        {
            // Clubs outside a known league get no allocation, and no cap.
            if (!allocations.TryGetValue(club.Id, out var increase))
                continue;

            var before = club.TransferBudget;
            club.TransferBudget = SeasonalBudgetRules.NextBudget(before, increase, isAIClub: club.Id != State.MyClubId);

            if (club.Id == State.MyClubId)
                (myIncrease, myReturned) = (increase, (int)Math.Max(0, (long)before + increase - club.TransferBudget));
        }

        return (myIncrease, myReturned);
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
            // A tier can hold several parallel leagues (e.g. Division 6 North and South).
            var tiers = countryLeagues
                .GroupBy(p => p.Rank)
                .OrderBy(p => p.Key)
                .Select(p => p.OrderBy(l => l.Id).ToList())
                .ToList();

            for (var i = 0; i < tiers.Count; i++)
            {
                foreach (var league in tiers[i])
                {
                    if (i > 0)
                    {
                        var leagueAbove = tiers[i - 1][0];

                        foreach (var club in league.PromotedClubs())
                            clubsById[club.Id].LeagueId = leagueAbove.Id;
                    }

                    if (i < tiers.Count - 1)
                    {
                        foreach (var (club, leagueBelow) in SplitAcrossLeagues(league.RelegatedClubs(tiers[i + 1][0]), tiers[i + 1]))
                            clubsById[club.Id].LeagueId = leagueBelow.Id;
                    }
                }
            }
        }
    }

    // Shuffles the clubs and deals them out in turn, so each league gets an even share.
    private static IEnumerable<(ClubModel club, LeagueModel league)> SplitAcrossLeagues(List<ClubModel> clubs, List<LeagueModel> leagues) =>
        clubs
            .OrderBy(_ => RandomNumberHelper.Next())
            .Select((club, index) => (club, leagues[index % leagues.Count]));

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
            player.FreeAgentSince = State.Date;
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

    // Out-of-contract players at or past the retirement age won't sign again, so they leave the game,
    // as do free agents who have gone a whole season without a club. That keeps the number of players
    // in the game from outgrowing the squads, since the youth intake adds more each year than retire.
    private void RetirePlayers()
    {
        foreach (var player in State.Players.Where(p => p.Contract == null && p.FreeAgentSince == null))
            player.FreeAgentSince = State.Date;

        var retiredIds = State.Players
            .Where(p => p.Contract == null && (p.IsRetiring(State.Date) || p.HasGivenUp(State.Date)))
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

    public const int YouthIntakePerClub = 2;

    // Every club promotes two 16-17 year olds from its academy on 1 March.
    private void ProcessYouthIntake()
    {
        var generator = new PlayerGenerator.PlayerGenerator();
        var startDate = State.Date.ToString("yyyy-MM-dd");
        var nextId = State.Players.Max(p => p.Id) + 1;
        var myIntake = new List<PlayerModel>();

        var leagueRankLookup = Settings.Competitions
            .Where(c => c.IsLeague)
            .ToDictionary(c => c.Id, c => c.Rank);

        foreach (var club in State.Clubs)
        {
            var leagueRank = leagueRankLookup.GetValueOrDefault(club.LeagueId, 1);
            var generated = generator.GenerateYouthIntake(club.Name, YouthIntakePerClub, startDate, leagueRank, club.AcademyRating);

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

    // A last resort, so generated players don't add to the number in the game: AI clubs short of a
    // matchday squad or two keepers sign free agents first (see TransferListHelper), and only what's
    // still missing is generated. Clubs build beyond this by signing free agents during the season.
    private void TopUpSquads(int squadSize)
    {
        var generator = new PlayerGenerator.PlayerGenerator();
        var startDate = State.Date.ToString("yyyy-MM-dd");
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

            var leagueRank = leagueRankLookup.GetValueOrDefault(club.LeagueId, 1);
            List<PlayerGenerator.GeneratedPlayer> generated;

            if (contractedPositions.Count < squadSize)
            {
                // Filled to the target, covering any position the squad has no one in.
                var missingCount = PlayerGenerator.PlayerGenerator.MissingPositions(contractedPositions).Count;
                var playersNeeded = Math.Max(squadSize - contractedPositions.Count, missingCount);
                generated = generator.GenerateTopUp(club.Name, playersNeeded, contractedPositions, startDate, leagueRank);
            }
            else
            {
                // A big enough squad is left to cover its own positions, except that it always has two keepers.
                var keepersNeeded = 2 - contractedPositions.Count(p => p == "GK");
                if (keepersNeeded <= 0)
                    continue;
                generated = generator.GenerateKeepers(club.Name, keepersNeeded, startDate, leagueRank);
            }

            // Generated numbers only avoid each other, so number the newcomers around the existing squad, best first.
            foreach (var gp in generated.OrderByDescending(p => p.Rating))
            {
                var player = MapToPlayerModel(gp, club);
                player.Id = nextId++;
                player.ShirtNumber = ShirtNumberRules.NextAvailable(State.Players, club.Id, player.Position);
                State.Players.Add(player);

                var reserveSlot = club.TacticSlots.FirstOrDefault(p => p.TacticSlotType == TacticSlotType.RES && p.PlayerId == null);
                if (reserveSlot != null)
                    reserveSlot.PlayerId = player.Id;
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

    // Last season's top-flight champions face the FA Cup winners; a double winner faces the league runners-up.
    private List<int>? ShieldQualifierIds()
    {
        var shield = Settings.Competitions.FirstOrDefault(p => p.Type == CompetitionType.EnglishCommunityShield.ToString());
        if (shield == null)
            return null;

        var leagueId = Settings.Competitions.FirstOrDefault(p => p.IsLeague && p.CountryId == shield.CountryId && p.Rank == 1)?.Id;

        var league = State.Competitions.OfType<LeagueModel>().FirstOrDefault(p => p.Id == leagueId);
        var table = league?.GenerateLeagueTable().Select(p => p.Club.Id).ToList();
        if (table == null || table.Count < 2)
            return null;

        var cupWinnerId = CupWinnerId(CompetitionType.EnglishFACup, shield.CountryId);

        var champions = table[0];
        var opponent = cupWinnerId is { } winner && winner != champions ? winner : table[1];
        return [champions, opponent];
    }

    private int? CupWinnerId(CompetitionType type, int countryId)
    {
        var cupId = Settings.Competitions.FirstOrDefault(p => p.Type == type.ToString() && p.CountryId == countryId)?.Id;
        var cup = State.Competitions.FirstOrDefault(p => p.Id == cupId);
        if (cup == null || cup.DrawSettings.Count == 0)
            return null;

        var finalRound = cup.DrawSettings.Max(p => p.Round);
        return cup.Fixtures
            .Where(p => p.Round == finalRound && p.Concluded && p.ClubWon != null)
            .Select(p => (int?)p.ClubWon!.Id)
            .FirstOrDefault();
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

        foreach (var competition in Settings.Competitions.OrderBy(p => p.CreationOrder()))
        {
            var competitionProvider = CompetitionFactories
                .First(p => p.Type.ToString() == competition.Type);

            var newCompetition = competitionProvider.CreateCompetition(competition);

            State.Competitions.Add(newCompetition);
        }

        if (shieldClubIds != null)
            ApplyShieldQualifiers(shieldClubIds);

        PlayoffScheduler.Schedule(State, EventManager, ServiceProvider);
        CompetitionEndEvent.ScheduleForCompetitions(State, EventManager, ServiceProvider);

        foreach (var competition in State.Competitions.Where(p => p.Type == CompetitionType.Friendly))
        {
            foreach (var drawDate in competition.DrawSettings)
                FriendlyService.GenerateNextRoundOfFixtures(competition);
        }

        RemoveDepartedPlayersFromTactics();
    }

    // Lineups carry over into the new season (AI clubs pick theirs afresh before each match anyway);
    // only players who have left a club over the summer are taken out of its slots.
    private void RemoveDepartedPlayersFromTactics()
    {
        var clubIdByPlayer = State.Players
            .Where(p => p.Contract != null)
            .ToDictionary(p => p.Id, p => p.Contract!.ClubId);

        foreach (var club in State.Clubs)
        {
            foreach (var slot in club.TacticSlots)
            {
                if (slot.PlayerId is { } playerId && clubIdByPlayer.GetValueOrDefault(playerId) != club.Id)
                    slot.PlayerId = null;
            }
        }
    }

    private void ValidateProcess()
    {
        var existsOutstandingFixtures = State.Competitions
            .SelectMany(p => p.Fixtures)
            .Any(p => !p.Concluded && p.Date == State.Date);

        if (existsOutstandingFixtures)
            throw new ProcessException(ScreenType.Fixture);
    }

    private void ProcessPhysios()
    {
        var club = State.Clubs.FirstOrDefault(c => c.Id == State.MyClubId);
        if (club == null) return;

        foreach (var physio in State.Physios.Where(p => State.Date >= p.NextPaymentDate).ToList())
        {
            physio.MonthsWorked++;

            if (club.TransferBudget >= PhysioRules.MonthlyWage)
            {
                club.TransferBudget -= PhysioRules.MonthlyWage;
                continue;
            }

            State.Physios.Remove(physio);
            NotificationFactory.AddNotification(
                State.Date,
                $"{physio.Name} (Physio)",
                "Physio Left",
                $"{physio.Name} has left the club because there was not enough in the budget to pay their £{PhysioRules.MonthlyWage:n0} monthly wage.");
        }
    }
}
