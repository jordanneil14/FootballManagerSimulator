using System.Globalization;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Exceptions;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Maui.Services;

/// <summary>
/// Central orchestration layer for the Blazor front-end. All of the game-flow logic that
/// previously lived inside the console screens' <c>HandleInput</c> methods is reproduced here
/// so the Razor components can stay thin (render + raise events). The underlying simulation
/// (helpers, factories, competition services, events) is used unchanged.
/// </summary>
public class GameEngine(
    IState state,
    IGameCreator gameCreator,
    IGameFactory gameFactory,
    IProcessHelper processHelper,
    IMatchSimulatorHelper matchSimulator,
    ITacticHelper tacticHelper,
    ITransferListHelper transferListHelper,
    IPlayerHelper playerHelper,
    IClubHelper clubHelper,
    IEventManager eventManager,
    IAppStorage appStorage,
    IServiceProvider serviceProvider)
{
    private readonly IAppStorage AppStorage = appStorage;

    public IState State { get; } = state;
    public IGameCreator GameCreator { get; } = gameCreator;
    public IMatchSimulatorHelper MatchSimulator { get; } = matchSimulator;

    /// <summary>
    /// Raised whenever the game state changes in a way that the persistent layout
    /// (top bar, side-nav) should reflect. Layout components subscribe to re-render.
    /// </summary>
    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();

    public void MarkLatestNotificationRead()
    {
        if (State.Notifications.Any())
        {
            var latest = State.Notifications[0];
            if (latest.RequiresAction) return;
            State.Notifications.RemoveAt(0);
        }
        NotifyChanged();
    }

    public IEnumerable<NotificationModel> UnreadNotifications =>
        State.Notifications.Where(p => p.Date <= State.Date);

    /// <summary>Removes every unread notification except those still awaiting a decision.</summary>
    public void ClearUnreadNotifications()
    {
        State.Notifications.RemoveAll(p => p.Date <= State.Date && !p.RequiresAction);
        NotifyChanged();
    }

    public bool HasBlockingNotification =>
        State.Notifications.Any(p => p.Date <= State.Date && p.RequiresAction);

    public void AcceptBid(NotificationModel notification)
    {
        if (notification.BidPlayerId == null || notification.BidAmount == null || notification.BiddingClubId == null) return;

        var player = GetPlayer(notification.BidPlayerId.Value);
        var biddingClub = GetClub(notification.BiddingClubId.Value);

        if (player?.Contract == null || player.Contract.ClubId != MyClub.Id || biddingClub == null)
        {
            notification.RequiresAction = false;
            notification.Response = "⚠️ Bid no longer valid";
            notification.Message = $"The bid for {player?.Name ?? "this player"} is no longer valid because he is no longer at your club.";
            NotifyChanged();
            return;
        }

        if (IsPlayerRetiring(player))
        {
            notification.RequiresAction = false;
            notification.Response = "⚠️ Bid fell through";
            notification.Message = $"The bid for {player.Name} has fallen through because he is retiring and will not sign a new contract.";
            NotifyChanged();
            return;
        }

        var oldSlot = MyClub.TacticSlots.FirstOrDefault(p => p.PlayerId == notification.BidPlayerId);
        if (oldSlot != null) oldSlot.PlayerId = null;

        MyClub.TransferBudget += notification.BidAmount.Value;
        biddingClub.TransferBudget -= notification.BidAmount.Value;

        player.Contract = new PlayerModel.ContractModel
        {
            ClubId = biddingClub.Id,
            ClubName = biddingClub.Name,
            ExpiryDate = new DateOnly(State.Date.Month < 7 ? State.Date.Year : State.Date.Year + 1, 6, 30).AddYears(1)
        };

        player.ShirtNumber = null; // now contracted to the buyers, so their old number mustn't count as taken
        player.ShirtNumber = ShirtNumberRules.NextAvailable(State.Players, biddingClub.Id, player.Position);

        if (transferListHelper.IsPlayerTransferListed(notification.BidPlayerId.Value))
            transferListHelper.RemovePlayerFromTransferList(notification.BidPlayerId.Value);

        notification.RequiresAction = false;
        notification.Response = "✅ Bid accepted";
        notification.Message = $"You accepted {biddingClub.Name}'s bid of £{notification.BidAmount.Value:n0} for {player.Name}. The fee has been added to your budget.";

        NotifyChanged();
    }

    public void RejectBid(NotificationModel notification)
    {
        if (notification.BidPlayerId == null || notification.BiddingClubId == null) return;

        var player = GetPlayer(notification.BidPlayerId.Value);
        var biddingClub = GetClub(notification.BiddingClubId.Value);

        notification.RequiresAction = false;
        notification.Response = "❌ Bid rejected";
        notification.Message = $"You rejected {biddingClub?.Name}'s bid of £{notification.BidAmount ?? 0:n0} for {player?.Name}.";

        NotifyChanged();
    }

    // Sacked managers carry on without a club. A job history tells that apart from a game still being set up.
    public bool IsUnemployed => State.MyClubId == null && State.ManagerHistory.Jobs.Count > 0 && State.Clubs.Count > 0;

    public bool GameInProgress => (State.MyClubId != null && State.Clubs.Any(p => p.Id == State.MyClubId)) || IsUnemployed;

    // Only use where a club is guaranteed, i.e. pages and actions that are hidden while unemployed.
    public ClubModel MyClub => State.Clubs.First(p => p.Id == State.MyClubId);

    public ClubModel? CurrentClub => State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId);

    // ---------------------------------------------------------------------------------
    // New-game flow (Welcome -> CreateManager -> SelectLeague -> SelectClub)
    // ---------------------------------------------------------------------------------

    public void StartNewGame()
    {
        ResetGame();
        gameFactory.IntitialiseGameState();
    }

    public void SetManagerName(string name)
    {
        var text = new CultureInfo("en-US", false).TextInfo;
        GameCreator.ManagerName = text.ToTitleCase(name.Trim().ToLower());
    }

    public void SelectLeague(int leagueId) => GameCreator.LeagueId = leagueId;

    public void SelectClub(int clubId)
    {
        GameCreator.ClubId = clubId;
        gameFactory.FinaliseGameState();
        NotifyChanged();
    }

    private void ResetGame()
    {
        State.MyClubId = null;
        State.MatchInProgress = false;
        State.Clubs.Clear();
        State.Players.Clear();
        State.Competitions.Clear();
        State.Notifications.Clear();
        State.TransferListItems.Clear();
        State.Events.Clear();
        State.UserFeedbackUpdates.Clear();
        State.ScreenStack.Clear();
        State.BoardConfidence = BoardConfidenceRules.Starting;
        State.ManagerHistory = new ManagerHistoryModel();
        State.Scouts.Clear();
        State.DiscoveredPlayerIds.Clear();
        State.CareerId = NewCareerId();
    }

    private static string NewCareerId() => Guid.NewGuid().ToString("N");

    // ---------------------------------------------------------------------------------
    // Day advancement
    // ---------------------------------------------------------------------------------

    public enum AdvanceResult { Advanced, MustPlayFixtures, BlockedByNotification }

    public AdvanceResult Advance()
    {
        if (HasBlockingNotification)
            return AdvanceResult.BlockedByNotification;

        try
        {
            // Catches a match day left at the full-time screen, so its cup ties still pay before the date moves on.
            PayCupPrizeMoney();
            processHelper.Process();
            // A new day has started, so yesterday's match is over however the user left it.
            State.MatchInProgress = false;
            // Saves are large, so quiet days only autosave once a week; match days save at full time.
            if (State.Date.DayOfWeek == AutosaveDay)
                AutoSave();
            return AdvanceResult.Advanced;
        }
        catch (ProcessException)
        {
            return AdvanceResult.MustPlayFixtures;
        }
        finally
        {
            NotifyChanged();
        }
    }

    // ---------------------------------------------------------------------------------
    // Match flow (Fixture -> PreMatch -> Match -> FullTime -> Results)
    // ---------------------------------------------------------------------------------

    public enum TodaysFixturesResult { PreMatch, Results }

    /// <summary>Reproduces the old Fixture screen "Advance" action.</summary>
    public TodaysFixturesResult AdvanceTodaysFixtures()
    {
        var includesMyClub = State.Competitions
            .SelectMany(p => p.Fixtures)
            .Any(p => p.Date == State.Date && (p.HomeClub.Id == State.MyClubId || p.AwayClub.Id == State.MyClubId));

        if (includesMyClub)
        {
            foreach (var fixture in TodaysFixtures())
                MatchSimulator.PrepareMatch(fixture);

            NotifyChanged();
            return TodaysFixturesResult.PreMatch;
        }

        foreach (var competition in State.Competitions)
        {
            foreach (var fixture in competition.Fixtures.Where(p => p.Date == State.Date))
            {
                MatchSimulator.PrepareMatch(fixture);
                MatchSimulator.ConcludeFixture(fixture, competition);
            }
        }

        PayCupPrizeMoney();
        NotifyChanged();
        return TodaysFixturesResult.Results;
    }

    /// <summary>
    /// Reproduces the old PreMatch screen "Start Match" action. Returns an error message when the
    /// team is not fully selected, otherwise kicks off the first half and returns null.
    /// </summary>
    public bool IsPlayerSuspended(int playerId)
    {
        var player = GetPlayer(playerId);
        return player != null && player.SuspensionMatches > 0;
    }

    public string? StartMatch()
    {
        var unfilledStarters = MyClub.TacticSlots
            .Where(p => p.TacticSlotType != TacticSlotType.SUB && p.TacticSlotType != TacticSlotType.RES)
            .Any(p => p.PlayerId == null);

        if (unfilledStarters)
            return "Unable to start game. Your team has not been fully selected.";

        var suspendedInSquad = MyClub.TacticSlots
            .Where(p => p.TacticSlotType != TacticSlotType.RES && p.PlayerId != null)
            .Any(p => IsPlayerSuspended(p.PlayerId!.Value));

        if (suspendedInSquad)
            return "Unable to start game. You have a suspended player in your starting lineup or substitutes.";

        var myFixture = MyFixtureToday();
        State.MatchInProgress = true;

        foreach (var competition in State.Competitions)
        {
            foreach (var fixture in competition.Fixtures.Where(p => p.Date == State.Date))
            {
                if (fixture == myFixture)
                {
                    MatchSimulator.PreGenerateMatch(fixture);
                }
                else
                {
                    MatchSimulator.ConcludeFixture(fixture, competition);
                }
            }
        }

        NotifyChanged();
        return null;
    }

    /// <summary>
    /// Reproduces the old Match screen "Continue Match" action. Returns true once the user's
    /// fixture has concluded (time for full-time), false if the match should continue.
    /// </summary>
    public bool ContinueMatch()
    {
        foreach (var competition in State.Competitions)
            foreach (var fixture in competition.Fixtures.Where(p => p.Date == State.Date))
                MatchSimulator.ProcessMatch(fixture, competition);

        NotifyChanged();

        var myFixture = MyFixtureToday();
        if (myFixture is { Concluded: true })
        {
            foreach (var competition in State.Competitions)
                foreach (var fixture in competition.Fixtures.Where(p => p.Date == State.Date && !p.Concluded))
                    MatchSimulator.ProcessMatch(fixture, competition);

            return true;
        }

        return false;
    }

    public ICompetition? GetCompetitionForFixture(FixtureModel fixture) =>
        State.Competitions.FirstOrDefault(p => p.Fixtures.Contains(fixture));

    public void FinalisePlayerMatch(FixtureModel fixture)
    {
        var competition = GetCompetitionForFixture(fixture);
        if (competition != null)
            MatchSimulator.FinaliseMatch(fixture, competition);
        NotifyChanged();
    }

    /// <summary>Reproduces the old FullTime screen "Continue" action.</summary>
    public void CompleteRemainingFixtures()
    {
        foreach (var competition in State.Competitions)
            foreach (var fixture in competition.Fixtures.Where(p => p.Date == State.Date && !p.Concluded))
                MatchSimulator.ProcessMatch(fixture, competition);

        State.MatchInProgress = false;
        PayCupPrizeMoney();
        AutoSave();
        NotifyChanged();
    }

    // English Cup ties pay both clubs, the winner double, but only once every match today is over,
    // so no money moves while the user's own game is still being played.
    private void PayCupPrizeMoney()
    {
        if (HasOutstandingFixturesToday())
            return;

        foreach (var cup in State.Competitions.Where(p => p.Type == CompetitionType.Cup))
        {
            var finalRound = cup.DrawSettings.Count == 0 ? 0 : cup.DrawSettings.Max(p => p.Round);
            var ties = cup.Fixtures.Where(p => p.Date == State.Date && p.Concluded && !p.PrizeMoneyPaid && p.ClubWon != null);

            foreach (var tie in ties)
            {
                tie.PrizeMoneyPaid = true;

                // Fixture clubs can be copies after a save/load, so pay the canonical State.Clubs entries.
                var winnerId = tie.ClubWon!.Id;
                var loserId = winnerId == tie.HomeClub.Id ? tie.AwayClub.Id : tie.HomeClub.Id;
                var winner = State.Clubs.FirstOrDefault(p => p.Id == winnerId);
                var loser = State.Clubs.FirstOrDefault(p => p.Id == loserId);
                var winnings = CupPrizeMoneyRules.ForWinner(tie.Round);
                var consolation = CupPrizeMoneyRules.ForLoser(tie.Round);

                if (winner != null)
                    winner.TransferBudget = (int)Math.Min((long)winner.TransferBudget + winnings, int.MaxValue);
                if (loser != null)
                    loser.TransferBudget = (int)Math.Min((long)loser.TransferBudget + consolation, int.MaxValue);

                var stage = $"{CupPrizeMoneyRules.RoundName(tie.Round, finalRound)} of the {cup.Name}";
                if (winnerId == State.MyClubId)
                    AddCupPrizeMoneyNotification(cup.Name,
                        $"Your win over {loser?.Name} in {stage} has earned £{winnings:n0} in prize money. It has been added to your budget.");
                else if (loserId == State.MyClubId)
                    AddCupPrizeMoneyNotification(cup.Name,
                        $"Your defeat to {winner?.Name} in {stage} still earned £{consolation:n0} in prize money. It has been added to your budget.");
            }
        }
    }

    private void AddCupPrizeMoneyNotification(string cupName, string message) =>
        State.Notifications.Add(new NotificationModel
        {
            Date = State.Date,
            Recipient = "Chairman",
            Subject = $"{cupName} Prize Money",
            Message = message
        });

    // ---------------------------------------------------------------------------------
    // Queries used by the UI
    // ---------------------------------------------------------------------------------

    public IEnumerable<FixtureModel> TodaysFixtures() => State.Competitions
        .SelectMany(p => p.Fixtures)
        .Where(p => p.Date == State.Date);

    public FixtureModel? MyFixtureToday() => State.Competitions
        .SelectMany(p => p.Fixtures)
        .FirstOrDefault(p => p.Date == State.Date && (p.HomeClub.Id == State.MyClubId || p.AwayClub.Id == State.MyClubId));

    public bool HasOutstandingFixturesToday() => State.Competitions
        .SelectMany(p => p.Fixtures)
        .Any(p => !p.Concluded && p.Date == State.Date);

    public int UnreadNotificationCount => State.Notifications.Count(p => p.Date <= State.Date);

    public ICompetition? MyCompetition => CurrentClub is { } club ? State.Competitions.FirstOrDefault(p => p.Id == club.LeagueId) : null;

    public IEnumerable<LeagueModel> Leagues => State.Competitions.Where(p => p.IsLeague).Cast<LeagueModel>();

    public string NextMatchCaption()
    {
        if (CurrentClub is not { } myClub) return "Out of work · find a club on the Jobs screen";

        var nextFixture = State.Competitions
            .SelectMany(p => p.Fixtures)
            .Where(p => p.Date >= State.Date && (p.HomeClub.Id == myClub.Id || p.AwayClub.Id == myClub.Id))
            .OrderBy(p => p.Date)
            .FirstOrDefault();

        if (nextFixture == null) return "Season Complete";

        var competition = State.Competitions.First(p => p.Fixtures.Contains(nextFixture));
        var clubAgainst = nextFixture.HomeClub.Id == myClub.Id ? nextFixture.AwayClub : nextFixture.HomeClub;

        if (nextFixture.Date == State.Date && nextFixture.Concluded)
            return $"Last match: {nextFixture.HomeClub.Name} {nextFixture.GoalsHome} - {nextFixture.GoalsAway} {nextFixture.AwayClub.Name}";

        if (nextFixture.Date == State.Date)
            return $"Next match: {competition.Name} vs {clubAgainst.Name} today";

        var diff = nextFixture.Date.DayNumber - State.Date.DayNumber;
        if (diff == 1) return $"Next match: {competition.Name} vs {clubAgainst.Name} tomorrow";

        return $"Next match: {competition.Name} vs {clubAgainst.Name} in {diff} days";
    }

    // ---------------------------------------------------------------------------------
    // Tactics
    // ---------------------------------------------------------------------------------

    public void SwapTacticSlots(int fromIndex, int toIndex)
    {
        var slots = MyClub.TacticSlots;
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= slots.Count || toIndex >= slots.Count) return;
        (slots[toIndex].PlayerId, slots[fromIndex].PlayerId) = (slots[fromIndex].PlayerId, slots[toIndex].PlayerId);
    }

    public const int MaxSubstitutions = 5;

    // Substitutions made in the match being played. The engine lives for the whole app session,
    // so this survives leaving the match screen and coming back.
    private FixtureModel? _substitutionFixture;
    private readonly HashSet<int> _playersBeenOnPitch = [];
    private readonly List<(int OffId, int OnId, int Minute)> _substitutions = [];

    private static bool IsOnPitch(TacticSlotModel slot) =>
        slot.TacticSlotType is not (TacticSlotType.SUB or TacticSlotType.RES);

    private void TrackSubstitutionsFor(FixtureModel fixture)
    {
        if (_substitutionFixture == fixture) return;
        _substitutionFixture = fixture;
        _substitutions.Clear();
        _playersBeenOnPitch.Clear();
        foreach (var slot in MyClub.TacticSlots.Where(s => IsOnPitch(s) && s.PlayerId != null))
            _playersBeenOnPitch.Add(slot.PlayerId!.Value);
    }

    public int SubstitutionsMade(FixtureModel fixture) =>
        _substitutionFixture == fixture ? _substitutions.Count : 0;

    /// <summary>
    /// Swaps two slots during a live match. In competitive matches bringing a bench player on
    /// uses one of <see cref="MaxSubstitutions"/> substitutions and a player taken off cannot
    /// return; swapping the same pair back at the same minute undoes the change. Friendlies have
    /// no limits. Returns why the swap was refused, or null when it was made.
    /// </summary>
    public string? MatchSwap(FixtureModel fixture, ICompetition? competition, int fromIndex, int toIndex, int minute)
    {
        var slots = MyClub.TacticSlots;
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= slots.Count || toIndex >= slots.Count) return null;

        if (competition?.Type == CompetitionType.Friendly)
        {
            SwapTacticSlots(fromIndex, toIndex);
            return null;
        }

        TrackSubstitutionsFor(fixture);

        var (from, to) = (slots[fromIndex], slots[toIndex]);
        if (IsOnPitch(from) == IsOnPitch(to))
        {
            SwapTacticSlots(fromIndex, toIndex); // positional change on the pitch, or a bench reshuffle
            return null;
        }

        var pitchSlot = IsOnPitch(from) ? from : to;
        var benchSlot = IsOnPitch(from) ? to : from;
        var goingOff = pitchSlot.PlayerId;
        var comingOn = benchSlot.PlayerId;

        if (comingOn == null)
            return "There is no player on the bench in that slot.";

        var undo = _substitutions.FindIndex(s => s.OnId == goingOff && s.OffId == comingOn && s.Minute == minute);
        if (undo >= 0)
        {
            _substitutions.RemoveAt(undo);
            _playersBeenOnPitch.Remove(goingOff!.Value);
            SwapTacticSlots(fromIndex, toIndex);
            return null;
        }

        if (_playersBeenOnPitch.Contains(comingOn.Value))
            return $"{GetPlayer(comingOn.Value)?.Name ?? "That player"} has already been substituted and cannot come back on.";

        if (_substitutions.Count >= MaxSubstitutions)
            return $"You have already made all {MaxSubstitutions} substitutions.";

        _substitutions.Add((goingOff ?? 0, comingOn.Value, minute));
        _playersBeenOnPitch.Add(comingOn.Value);
        SwapTacticSlots(fromIndex, toIndex);
        return null;
    }

    public void AutoPickTeam()
    {
        tacticHelper.ResetTacticForClub(MyClub);
        tacticHelper.FillEmptyTacticSlotsByClubId(MyClub.Id);
    }

    public void ResetTactic() => tacticHelper.ResetTacticForClub(MyClub);

    public void ChangeFormation(string formation)
    {
        var oldPlayerIds = MyClub.TacticSlots
            .Select(s => s.PlayerId)
            .ToList();

        MyClub.Formation = formation;
        tacticHelper.ResetTacticForClub(MyClub);

        for (var i = 0; i < Math.Min(oldPlayerIds.Count, MyClub.TacticSlots.Count); i++)
            MyClub.TacticSlots[i].PlayerId = oldPlayerIds[i];
    }

    // ---------------------------------------------------------------------------------
    // Transfer window
    // ---------------------------------------------------------------------------------

    public bool IsTransferWindowOpen => TransferWindowRules.IsOpen(State.Date);

    public string TransferWindowStatus => IsTransferWindowOpen ? "Open" : "Closed";

    public DateOnly NextTransferWindowDate
    {
        get
        {
            var date = State.Date;
            if (date.Month >= 2 && date.Month < 6)
                return new DateOnly(date.Year, 6, 15);
            if (date.Month == 6 && date.Day < 15)
                return new DateOnly(date.Year, 6, 15);
            if (date.Month >= 9)
                return new DateOnly(date.Year + 1, 1, 1);
            return date;
        }
    }

    // ---------------------------------------------------------------------------------
    // Transfers
    // ---------------------------------------------------------------------------------

    public string? BuyTransferListedPlayer(int playerId, int contractYears = 2)
    {
        if (!IsTransferWindowOpen)
            return "The transfer window is currently closed.";

        var item = transferListHelper.GetTransferListItemByPlayerId(playerId);
        if (item == null) return "This player is not on the transfer list.";
        if (RetirementRefusal(playerId) is { } refusal) return refusal;
        if (SigningLevelRefusal(playerId) is { } levelRefusal) return levelRefusal;
        if (item.AskingPrice > MyClub.TransferBudget) return "Insufficient funds to purchase this player.";

        transferListHelper.TransferContractedPlayerByPlayerIdAndClubId(item.PlayerId, MyClub.Id);

        var player = playerHelper.GetPlayerById(playerId);
        if (player?.Contract != null)
        {
            var expiryYear = State.Date.Month < 7 ? State.Date.Year : State.Date.Year + 1;
            player.Contract.ExpiryDate = new DateOnly(expiryYear, 6, 30).AddYears(contractYears - 1);
        }

        return player == null ? null : $"{player.Name} has been signed.";
    }

    public string? ListPlayerForTransfer(int playerId, int askingPrice)
    {
        if (!IsTransferWindowOpen)
            return "The transfer window is currently closed.";

        transferListHelper.AddPlayerToTransferList(playerId, askingPrice);
        return null;
    }

    public void RemovePlayerFromTransferList(int playerId) =>
        transferListHelper.RemovePlayerFromTransferList(playerId);

    public bool IsPlayerTransferListed(int playerId) => transferListHelper.IsPlayerTransferListed(playerId);

    public TransferListItemModel? GetTransferListItem(int playerId) =>
        transferListHelper.GetTransferListItemByPlayerId(playerId);

    public bool IsPlayerRetiring(PlayerModel player) => player.IsRetiring(State.Date);

    // Players at or past the retirement age won't sign a new deal.
    private string? RetirementRefusal(int playerId)
    {
        var player = playerHelper.GetPlayerById(playerId);
        return player != null && IsPlayerRetiring(player)
            ? $"{player.Name} is retiring and will not sign a new contract."
            : null;
    }

    // Renewals can happen at any time; the new deal runs from the end of the current season like any other contract.
    public string RenewContract(int playerId, int contractYears)
    {
        var player = playerHelper.GetPlayerById(playerId);
        if (player?.Contract == null || player.Contract.ClubId != MyClub.Id)
            return "This player is not contracted to your club.";

        if (RetirementRefusal(playerId) is { } refusal) return refusal;

        var expiryYear = State.Date.Month < 7 ? State.Date.Year : State.Date.Year + 1;
        var expiryDate = new DateOnly(expiryYear, 6, 30).AddYears(contractYears - 1);
        if (expiryDate <= player.Contract.ExpiryDate)
            return $"{player.Name}'s current contract already runs until {player.Contract.ExpiryDate:dd MMM yyyy}.";

        player.Contract.ExpiryDate = expiryDate;
        NotifyChanged();
        return $"{player.Name} has signed a new contract until {expiryDate:dd MMM yyyy}.";
    }

    public const int SigningRatingRange = 5;
    public const int SquadAverageBestPlayers = 20;

    // Judged on the best players only, so a big squad padded with youngsters and reserves isn't held against you.
    public double? MySquadAverageRating()
    {
        if (CurrentClub is not { } club) return null;
        var ratings = State.Players
            .Where(p => p.Contract?.ClubId == club.Id)
            .Select(p => p.Rating)
            .OrderByDescending(r => r)
            .Take(SquadAverageBestPlayers)
            .ToList();
        return ratings.Count == 0 ? null : ratings.Average();
    }

    // No player will drop to a club well below their level; weaker players will always join.
    // Your own players are never refused, since they've already signed.
    public string? SigningLevelRefusal(PlayerModel player)
    {
        var appliesToPlayer = player.Contract == null || player.Contract.ClubId != State.MyClubId;
        if (!appliesToPlayer || MySquadAverageRating() is not { } average || player.Rating - average <= SigningRatingRange)
            return null;

        return $"{player.Name} doesn't think your squad is at his level and has turned down the move.";
    }

    private string? SigningLevelRefusal(int playerId) =>
        playerHelper.GetPlayerById(playerId) is { } player ? SigningLevelRefusal(player) : null;

    public string SignFreeAgent(int playerId, int contractYears)
    {
        if (RetirementRefusal(playerId) is { } refusal) return refusal;
        if (SigningLevelRefusal(playerId) is { } levelRefusal) return levelRefusal;

        transferListHelper.SignFreeAgentByPlayerId(playerId, contractYears);
        var player = playerHelper.GetPlayerById(playerId);
        return player == null ? "Player signed." : $"{player.Name} has been signed.";
    }

    public BidResultModel SubmitBid(int playerId, int bidAmount)
    {
        var player = playerHelper.GetPlayerById(playerId);
        if (player == null)
            return new BidResultModel { Message = "Player not found." };

        if (RetirementRefusal(playerId) is { } refusal)
            return new BidResultModel { Message = refusal };

        if (SigningLevelRefusal(player) is { } levelRefusal)
            return new BidResultModel { Message = levelRefusal };

        if (!IsTransferWindowOpen)
            return new BidResultModel { Message = "The transfer window is currently closed." };

        if (bidAmount > MyClub.TransferBudget)
            return new BidResultModel { Message = "Insufficient budget." };

        var transferValue = playerHelper.GetTransferValue(player);
        // A listed player's club holds out for its asking price; otherwise it wants a premium on his value.
        var askingPrice = transferListHelper.GetTransferListItemByPlayerId(playerId)?.AskingPrice;
        var minimumAcceptable = askingPrice ?? (int)(transferValue * 1.3);
        var counterAmount = minimumAcceptable;

        if (bidAmount >= minimumAcceptable)
        {
            return new BidResultModel
            {
                Accepted = true,
                Message = $"{player.Contract?.ClubName ?? "The club"} have accepted your bid of {Ui.Money(bidAmount)} for {player.Name}."
            };
        }

        if (bidAmount >= (askingPrice ?? transferValue) * 0.5)
        {
            return new BidResultModel
            {
                CounterOffer = counterAmount,
                Message = $"{player.Contract?.ClubName ?? "The club"} rejected your bid but would accept {Ui.Money(counterAmount)}."
            };
        }

        return new BidResultModel
        {
            Message = $"{player.Contract?.ClubName ?? "The club"} rejected your bid outright. It was too low."
        };
    }

    public string CompletePurchase(int playerId, int fee, int contractYears)
    {
        var player = playerHelper.GetPlayerById(playerId);
        if (player?.Contract == null) return "Player not found.";
        if (RetirementRefusal(playerId) is { } refusal) return refusal;
        if (SigningLevelRefusal(player) is { } levelRefusal) return levelRefusal;

        var sellingClubId = player.Contract.ClubId;
        var sellingClub = clubHelper.GetClubById(sellingClubId);

        var oldSlot = sellingClub.TacticSlots.FirstOrDefault(p => p.PlayerId == playerId);
        if (oldSlot != null) oldSlot.PlayerId = null;

        sellingClub.TransferBudget += fee;
        MyClub.TransferBudget -= fee;

        var expiryYear = State.Date.Month < 7 ? State.Date.Year : State.Date.Year + 1;
        var expiryDate = new DateOnly(expiryYear, 6, 30).AddYears(contractYears - 1);

        player.Contract = new PlayerModel.ContractModel
        {
            ClubId = MyClub.Id,
            ClubName = MyClub.Name,
            ExpiryDate = expiryDate
        };

        player.ShirtNumber = null; // already contracted to us, so their old number mustn't count as taken
        player.ShirtNumber = ShirtNumberRules.NextAvailable(State.Players, MyClub.Id, player.Position);

        MyClub.TacticSlots
            .First(p => p.TacticSlotType == Enums.TacticSlotType.RES && p.PlayerId == null)
            .PlayerId = playerId;

        // Remove from transfer list if present
        if (transferListHelper.IsPlayerTransferListed(playerId))
            transferListHelper.RemovePlayerFromTransferList(playerId);

        NotifyChanged();
        return $"{player.Name} has signed for {Ui.Money(fee)}!";
    }

    // ---------------------------------------------------------------------------------
    // Finances
    // ---------------------------------------------------------------------------------

    public string RequestHigherTransferBudget()
    {
        var request = ActivatorUtilities.CreateInstance<IncreaseTransferBudgetRequestEvent>(serviceProvider, State.Date.AddDays(2), false);
        var (success, errorMessage) = request.ValidateAdd();
        if (!success) return errorMessage;
        State.Events.Add(request);
        return "Budget request has been submitted.";
    }

    public StadiumExpansionEvent? StadiumExpansionInProgress =>
        State.Events.OfType<StadiumExpansionEvent>().FirstOrDefault(e => !e.IsCompleted);

    public int? NextStadiumExpansionCost =>
        MyClub.StadiumSize >= StadiumRules.MaxCapacity ? null : StadiumRules.UpgradeCost(MyClub.StadiumSize);

    // Paid for out of the budget up front; the new capacity applies once the work finishes.
    public string ExpandStadium()
    {
        if (NextStadiumExpansionCost is not { } cost)
            return $"Your stadium is already at the maximum capacity of {StadiumRules.MaxCapacity:n0}.";

        if (cost > MyClub.TransferBudget)
            return $"The board cannot fund this. Expanding the stadium costs {Ui.Money(cost)}.";

        var completionDate = State.Date.AddMonths(StadiumRules.UpgradeMonths);
        var expansion = ActivatorUtilities.CreateInstance<StadiumExpansionEvent>(serviceProvider, completionDate, false);
        var (success, errorMessage) = expansion.ValidateAdd();
        if (!success) return errorMessage;

        State.Events.Add(expansion);
        MyClub.TransferBudget -= cost;
        NotifyChanged();
        return $"The board has approved the stadium expansion for {Ui.Money(cost)}. Work will finish on {completionDate:dd MMM yyyy}.";
    }

    public AcademyUpgradeEvent? AcademyUpgradeInProgress =>
        State.Events.OfType<AcademyUpgradeEvent>().FirstOrDefault(e => !e.IsCompleted);

    public int? NextAcademyUpgradeCost =>
        MyClub.AcademyRating >= AcademyRules.Max ? null : AcademyRules.UpgradeCost(MyClub.AcademyRating + 1);

    // Paid for out of the budget up front; the new rating applies once the work finishes.
    public string UpgradeAcademy()
    {
        if (NextAcademyUpgradeCost is not { } cost)
            return $"Your academy is already rated the maximum of {AcademyRules.Max}.";

        if (cost > MyClub.TransferBudget)
            return $"The board cannot fund this. Upgrading the academy costs {Ui.Money(cost)}.";

        var completionDate = State.Date.AddMonths(AcademyRules.UpgradeMonths);
        var upgrade = ActivatorUtilities.CreateInstance<AcademyUpgradeEvent>(serviceProvider, completionDate, false);
        var (success, errorMessage) = upgrade.ValidateAdd();
        if (!success) return errorMessage;

        State.Events.Add(upgrade);
        MyClub.TransferBudget -= cost;
        NotifyChanged();
        return $"The board has approved the academy upgrade for {Ui.Money(cost)}. Work will finish on {completionDate:dd MMM yyyy}.";
    }

    // ---------------------------------------------------------------------------------
    // Player / club helpers surfaced to the UI
    // ---------------------------------------------------------------------------------

    public PlayerModel? GetPlayer(int id) => playerHelper.GetPlayerById(id);
    public int GetTransferValue(PlayerModel player) => playerHelper.GetTransferValue(player);
    public ClubModel? GetClub(int id) => clubHelper.GetClubById(id);
    public bool PlayerPlaysForMyClub(int playerId) =>
        State.MyClubId is { } myClubId && playerHelper.DoesPlayerPlaysForClub(playerId, myClubId);

    // ---------------------------------------------------------------------------------
    // Save / load
    // ---------------------------------------------------------------------------------

    public string SaveDirectory => AppStorage.SaveDirectory;

    public const string AutosavePrefix = "autosave-";
    public const DayOfWeek AutosaveDay = DayOfWeek.Monday;

    public string AutosaveFileName => $"{AutosavePrefix}{State.CareerId}.fms";

    // Engines are scoped, but they all share the one State, so saves and loads take a shared lock.
    private static readonly object SaveLock = new();

    public const string SaveBlockedDuringMatchMessage = "You can't save during a match. Finish the match first.";

    // A match in progress is partly held by the Match page, so no saving from kick-off until it has
    // finished. Checked against today's fixture too, so a flag left behind (e.g. leaving the finished
    // match without pressing Continue) can never block saving afterwards.
    public bool CanSave => !(State.MatchInProgress && MyFixtureToday() is { Concluded: false });

    public string SaveGame(string fileName)
    {
        if (!CanSave)
            return SaveBlockedDuringMatchMessage;

        try
        {
            lock (SaveLock)
                WriteSave(Path.Combine(SaveDirectory, $"{fileName}.fms"));
            return "Game saved successfully.";
        }
        catch (Exception)
        {
            return "Unable to save game.";
        }
    }

    /// <summary>
    /// Writes the career to its own autosave slot. Never runs during a match, since a match in
    /// progress is partly held by the Match page. Never throws; a failure is reported as feedback.
    /// </summary>
    public bool AutoSave()
    {
        if (!State.AutoSaveEnabled || !GameInProgress || !CanSave)
            return false;

        // Skip rather than wait if a load or another save is running, e.g. the app stopping mid-load.
        if (!Monitor.TryEnter(SaveLock))
            return false;

        try
        {
            if (string.IsNullOrEmpty(State.CareerId))
                State.CareerId = NewCareerId();

            WriteSave(Path.Combine(SaveDirectory, AutosaveFileName));
            return true;
        }
        catch (Exception)
        {
            State.UserFeedbackUpdates.Add("Autosave failed. Your last save is unchanged.");
            return false;
        }
        finally
        {
            Monitor.Exit(SaveLock);
        }
    }

    // Writes beside the target and then swaps it in, so a crash mid-write never corrupts an existing save.
    private void WriteSave(string path)
    {
        var tempPath = path + ".tmp";
        using (var writer = new StreamWriter(tempPath))
            SaveSerializer.Create().Serialize(writer, State);
        File.Move(tempPath, path, overwrite: true);
    }

    public IEnumerable<LoadGamePreviewModel> ListSaves()
    {
        var games = new List<LoadGamePreviewModel>();
        FileInfo[] files;
        try
        {
            files = new DirectoryInfo(SaveDirectory).GetFiles("*.fms");
        }
        catch (Exception)
        {
            return games;
        }

        foreach (var file in files)
        {
            try
            {
                var (clubName, managerName) = ReadSavePreview(file.FullName);
                games.Add(new LoadGamePreviewModel
                {
                    FileName = file.Name,
                    ClubName = clubName,
                    ManagerName = managerName,
                    SaveDate = file.LastWriteTime,
                    IsAutosave = file.Name.StartsWith(AutosavePrefix)
                });
            }
            catch (Exception)
            {
                // Ignore unreadable save files.
            }
        }

        return games.OrderByDescending(p => p.SaveDate);
    }

    // Permanently removes a save from the saves folder. Returns an error message, or null on success.
    public string? DeleteSave(string fileName)
    {
        if (Path.GetFileName(fileName) != fileName || Path.GetExtension(fileName) != ".fms")
            return "That save could not be found.";

        var path = Path.Combine(SaveDirectory, fileName);
        try
        {
            lock (SaveLock)
            {
                if (!File.Exists(path))
                    return "That save could not be found.";
                File.Delete(path);
            }
            return null;
        }
        catch (Exception)
        {
            return "Unable to delete the save.";
        }
    }

    // Single streaming pass; Events stay as JSON because they need DI to be constructed.
    private static (StateModel State, JArray? EventsJson) ReadSave(string path)
    {
        using var reader = new JsonTextReader(new StreamReader(path));
        var serializer = SaveSerializer.Create();
        var state = new StateModel();
        JArray? eventsJson = null;

        if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
            throw new JsonException("Not a save file.");

        while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
        {
            var name = (string)reader.Value!;
            reader.Read();

            if (name == nameof(StateModel.Events))
            {
                eventsJson = reader.TokenType == JsonToken.StartArray ? JArray.Load(reader) : null;
                continue;
            }

            var property = typeof(StateModel).GetProperty(name);
            if (property == null || !property.CanWrite)
            {
                reader.Skip();
                continue;
            }

            var value = serializer.Deserialize(reader, property.PropertyType);
            if (value != null || !property.PropertyType.IsValueType)
                property.SetValue(state, value);
        }

        return (state, eventsJson);
    }

    // Streams only the top-level fields the preview needs, skipping players/competitions without allocating them.
    private static (string ClubName, string ManagerName) ReadSavePreview(string path)
    {
        using var reader = new JsonTextReader(new StreamReader(path));
        var clubNames = new Dictionary<int, string>();
        int? myClubId = null;
        string? managerName = null;

        if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
            throw new JsonException("Not a save file.");

        while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
        {
            var property = (string)reader.Value!;
            reader.Read();

            switch (property)
            {
                case nameof(StateModel.ManagerName):
                    managerName = reader.Value as string;
                    break;
                case nameof(StateModel.MyClubId):
                    myClubId = reader.Value == null ? null : Convert.ToInt32(reader.Value);
                    break;
                case nameof(StateModel.Clubs) when reader.TokenType == JsonToken.StartArray:
                    ReadClubNames(reader, clubNames);
                    break;
                default:
                    reader.Skip();
                    break;
            }

            if (managerName != null && myClubId != null && clubNames.Count > 0)
                break;
        }

        var clubName = myClubId != null && clubNames.TryGetValue(myClubId.Value, out var name) ? name : "";
        return (clubName, managerName ?? "");
    }

    private static void ReadClubNames(JsonTextReader reader, Dictionary<int, string> clubNames)
    {
        while (reader.Read() && reader.TokenType == JsonToken.StartObject)
        {
            int? id = null;
            string? name = null;

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var property = (string)reader.Value!;
                reader.Read();

                if (property == nameof(ClubModel.Id))
                    id = Convert.ToInt32(reader.Value);
                else if (property == nameof(ClubModel.Name))
                    name = reader.Value as string;
                else
                    reader.Skip();
            }

            if (id != null && name != null)
                clubNames[id.Value] = name;
        }
    }

    // Competitions and fixtures are saved holding club references; point them back at the real clubs.
    private void RelinkClubs()
    {
        var clubsById = State.Clubs.ToDictionary(p => p.Id);
        ClubModel Real(ClubModel club) => clubsById.GetValueOrDefault(club.Id, club);

        foreach (var competition in State.Competitions)
        {
            competition.Clubs = competition.Clubs.Select(Real).ToList();
            foreach (var fixture in competition.Fixtures)
            {
                fixture.HomeClub = Real(fixture.HomeClub);
                fixture.AwayClub = Real(fixture.AwayClub);
                if (fixture.ClubWon != null)
                    fixture.ClubWon = Real(fixture.ClubWon);
            }
        }
    }

    public string? LoadGame(string fileName)
    {
        lock (SaveLock)
            return LoadGameLocked(fileName);
    }

    private string? LoadGameLocked(string fileName)
    {
        try
        {
            var (deserialised, eventsJson) = ReadSave(Path.Combine(SaveDirectory, fileName));

            State.Weather = deserialised.Weather;
            State.ScreenStack.Clear();
            State.Notifications = deserialised.Notifications;
            State.ManagerName = deserialised.ManagerName;
            State.Clubs = deserialised.Clubs;
            State.Date = deserialised.Date;
            State.MyClubId = deserialised.MyClubId;
            State.Players = deserialised.Players;
            State.Competitions = deserialised.Competitions;
            State.UserFeedbackUpdates = deserialised.UserFeedbackUpdates;
            State.TransferListItems = deserialised.TransferListItems;
            State.BoardConfidence = deserialised.BoardConfidence;
            State.ManagerHistory = deserialised.ManagerHistory;
            State.Scouts = deserialised.Scouts;
            State.DiscoveredPlayerIds = deserialised.DiscoveredPlayerIds;
            State.MatchSpeedMs = deserialised.MatchSpeedMs;
            State.AutoSaveEnabled = deserialised.AutoSaveEnabled;
            State.MatchInProgress = false;
            // Saves from before autosave existed get their own slot from here on.
            State.CareerId = string.IsNullOrEmpty(deserialised.CareerId) ? NewCareerId() : deserialised.CareerId;

            RelinkClubs();

            // Saves from before the manager history existed start the record from today.
            var myClub = State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId);
            if (State.ManagerHistory.CurrentJob == null && myClub != null)
                State.ManagerHistory.StartJob(myClub, State.Date);

            // Saves from before academies existed hand every club its starting rating.
            foreach (var club in State.Clubs.Where(p => p.AcademyRating == 0))
            {
                var leagueRank = State.Competitions.OfType<LeagueModel>().FirstOrDefault(p => p.Id == club.LeagueId)?.Rank ?? 1;
                club.AcademyRating = AcademyRules.StartingRating(leagueRank);
            }

            State.Events.Clear();
            if (eventsJson != null)
            {
                foreach (var eventToken in eventsJson)
                {
                    try
                    {
                        var typeName = eventToken["$type"]?.Value<string>();
                        if (typeName == null) continue;

                        var eventType = Type.GetType(typeName);
                        if (eventType == null) continue;

                        var triggerDate = eventToken["TriggerDate"]?.ToObject<DateOnly>() ?? default;
                        var isCompleted = eventToken["IsCompleted"]?.Value<bool>() ?? false;

                        var args = new List<object> { triggerDate, isCompleted };

                        var compIdToken = eventToken["CompetitionId"];
                        if (compIdToken != null)
                            args.Add(compIdToken.Value<int>());

                        var gameEvent = (IEvent)ActivatorUtilities.CreateInstance(
                            serviceProvider, eventType, args.ToArray());
                        eventManager.ValidateAndAddEvent(gameEvent);
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
