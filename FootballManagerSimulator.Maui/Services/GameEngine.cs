using System.Globalization;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Exceptions;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
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
            notification.Message = $"The bid for {player?.Name ?? "this player"} is no longer valid because he is no longer at your club.";
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

        if (transferListHelper.IsPlayerTransferListed(notification.BidPlayerId.Value))
            transferListHelper.RemovePlayerFromTransferList(notification.BidPlayerId.Value);

        notification.RequiresAction = false;
        notification.Message = $"You accepted {biddingClub.Name}'s bid of £{notification.BidAmount.Value:n0} for {player.Name}. The fee has been added to your transfer budget.";

        NotifyChanged();
    }

    public void RejectBid(NotificationModel notification)
    {
        if (notification.BidPlayerId == null || notification.BiddingClubId == null) return;

        var player = GetPlayer(notification.BidPlayerId.Value);
        var biddingClub = GetClub(notification.BiddingClubId.Value);

        notification.RequiresAction = false;
        notification.Message = $"You rejected {biddingClub?.Name}'s bid of £{notification.BidAmount ?? 0:n0} for {player?.Name}.";

        NotifyChanged();
    }

    public bool GameInProgress => State.MyClubId != null && State.Clubs.Any(p => p.Id == State.MyClubId);

    public ClubModel MyClub => State.Clubs.First(p => p.Id == State.MyClubId);

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
        State.Clubs.Clear();
        State.Players.Clear();
        State.Competitions.Clear();
        State.Notifications.Clear();
        State.TransferListItems.Clear();
        State.Events.Clear();
        State.UserFeedbackUpdates.Clear();
        State.ScreenStack.Clear();
    }

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
            processHelper.Process();
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
                matchSimulator.PrepareMatch(fixture);

            NotifyChanged();
            return TodaysFixturesResult.PreMatch;
        }

        foreach (var competition in State.Competitions)
        {
            foreach (var fixture in competition.Fixtures.Where(p => p.Date == State.Date))
            {
                matchSimulator.PrepareMatch(fixture);
                matchSimulator.ConcludeFixture(fixture, competition);
            }
        }

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

        var suspendedStarter = MyClub.TacticSlots
            .Where(p => p.TacticSlotType != TacticSlotType.SUB && p.TacticSlotType != TacticSlotType.RES && p.PlayerId != null)
            .Any(p => IsPlayerSuspended(p.PlayerId!.Value));

        if (suspendedStarter)
            return "Unable to start game. You have a suspended player in your starting lineup.";

        var myFixture = MyFixtureToday();

        foreach (var competition in State.Competitions)
        {
            foreach (var fixture in competition.Fixtures.Where(p => p.Date == State.Date))
            {
                if (fixture == myFixture)
                {
                    matchSimulator.PreGenerateMatch(fixture);
                }
                else
                {
                    matchSimulator.ConcludeFixture(fixture, competition);
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
                matchSimulator.ProcessMatch(fixture, competition);

        NotifyChanged();

        var myFixture = MyFixtureToday();
        if (myFixture is { Concluded: true })
        {
            foreach (var competition in State.Competitions)
                foreach (var fixture in competition.Fixtures.Where(p => p.Date == State.Date && !p.Concluded))
                    matchSimulator.ProcessMatch(fixture, competition);

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
            matchSimulator.FinaliseMatch(fixture, competition);
        NotifyChanged();
    }

    /// <summary>Reproduces the old FullTime screen "Continue" action.</summary>
    public void CompleteRemainingFixtures()
    {
        foreach (var competition in State.Competitions)
            foreach (var fixture in competition.Fixtures.Where(p => p.Date == State.Date && !p.Concluded))
                matchSimulator.ProcessMatch(fixture, competition);

        NotifyChanged();
    }

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

    public ICompetition? MyCompetition => State.Competitions.FirstOrDefault(p => p.Id == MyClub.LeagueId);

    public IEnumerable<LeagueModel> Leagues => State.Competitions.Where(p => p.IsLeague).Cast<LeagueModel>();

    public string NextMatchCaption()
    {
        var myClub = MyClub;
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

    public bool IsTransferWindowOpen
    {
        get
        {
            var month = State.Date.Month;
            var day = State.Date.Day;
            return (month >= 6 && month <= 8 && !(month == 6 && day < 15))
                || (month == 1);
        }
    }

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

    public string SignFreeAgent(int playerId, int contractYears)
    {
        transferListHelper.SignFreeAgentByPlayerId(playerId, contractYears);
        var player = playerHelper.GetPlayerById(playerId);
        return player == null ? "Player signed." : $"{player.Name} has been signed.";
    }

    public BidResultModel SubmitBid(int playerId, int bidAmount)
    {
        var player = playerHelper.GetPlayerById(playerId);
        if (player == null)
            return new BidResultModel { Message = "Player not found." };

        if (!IsTransferWindowOpen)
            return new BidResultModel { Message = "The transfer window is currently closed." };

        if (bidAmount > MyClub.TransferBudget)
            return new BidResultModel { Message = "Insufficient transfer budget." };

        var transferValue = playerHelper.GetTransferValue(player);
        var isTransferListed = transferListHelper.IsPlayerTransferListed(playerId);
        var minimumAcceptable = isTransferListed ? (int)(transferValue * 0.9) : (int)(transferValue * 1.3);
        var counterAmount = isTransferListed ? (int)(transferValue * 1.0) : (int)(transferValue * 1.3);

        if (bidAmount >= minimumAcceptable)
        {
            return new BidResultModel
            {
                Accepted = true,
                Message = $"{player.Contract?.ClubName ?? "The club"} have accepted your bid of {Ui.Money(bidAmount)} for {player.Name}."
            };
        }

        if (bidAmount >= transferValue * 0.5)
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

        var takenNumbers = State.Players
            .Where(p => p.Contract != null && p.Contract.ClubId == MyClub.Id && p.ShirtNumber.HasValue)
            .Select(p => p.ShirtNumber!.Value)
            .ToHashSet();
        var gkNumbers = new HashSet<int> { 1, 12, 13, 21 };
        var isGk = player.Position == "GK";
        var candidates = Enumerable.Range(1, 99)
            .Where(n => !takenNumbers.Contains(n))
            .Where(n => isGk ? gkNumbers.Contains(n) : !gkNumbers.Contains(n))
            .ToList();
        if (candidates.Count == 0)
            candidates = Enumerable.Range(1, 99).Where(n => !takenNumbers.Contains(n)).ToList();
        player.ShirtNumber = candidates[0];

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
        return "Transfer budget request has been submitted.";
    }

    public string RequestStadiumExpansion()
    {
        var request = ActivatorUtilities.CreateInstance<StadiumExpansionRequestEvent>(serviceProvider, State.Date.AddDays(2), false);
        var (success, errorMessage) = request.ValidateAdd();
        if (!success) return errorMessage;
        State.Events.Add(request);
        return "Stadium expansion request has been submitted.";
    }

    // ---------------------------------------------------------------------------------
    // Player / club helpers surfaced to the UI
    // ---------------------------------------------------------------------------------

    public PlayerModel? GetPlayer(int id) => playerHelper.GetPlayerById(id);
    public int GetTransferValue(PlayerModel player) => playerHelper.GetTransferValue(player);
    public ClubModel? GetClub(int id) => clubHelper.GetClubById(id);
    public bool PlayerPlaysForMyClub(int playerId) => playerHelper.DoesPlayerPlaysForClub(playerId, MyClub.Id);

    // ---------------------------------------------------------------------------------
    // Save / load
    // ---------------------------------------------------------------------------------

    public string SaveDirectory => AppStorage.SaveDirectory;

    public string SaveGame(string fileName)
    {
        try
        {
            var stateAsJson = JsonConvert.SerializeObject(State, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            File.WriteAllText(Path.Combine(SaveDirectory, $"{fileName}.fms"), stateAsJson);
            return "Game saved successfully.";
        }
        catch (Exception)
        {
            return "Unable to save game.";
        }
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
                    SaveDate = file.LastWriteTime
                });
            }
            catch (Exception)
            {
                // Ignore unreadable save files.
            }
        }

        return games.OrderByDescending(p => p.SaveDate);
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

    public string? LoadGame(string fileName)
    {
        try
        {
            var fileContent = File.ReadAllText(Path.Combine(SaveDirectory, fileName));

            var json = JObject.Parse(fileContent);
            var eventsJson = json["Events"] as JArray;
            json.Remove("Events");

            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            var deserialised = json.ToObject<StateModel>(JsonSerializer.Create(settings));
            if (deserialised == null) return "Unable to load game.";

            State.Weather = deserialised.Weather;
            State.ScreenStack = deserialised.ScreenStack;
            State.Notifications = deserialised.Notifications;
            State.ManagerName = deserialised.ManagerName;
            State.Clubs = deserialised.Clubs;
            State.Date = deserialised.Date;
            State.MyClubId = deserialised.MyClubId;
            State.Players = deserialised.Players;
            State.Competitions = deserialised.Competitions;
            State.UserFeedbackUpdates = deserialised.UserFeedbackUpdates;
            State.TransferListItems = deserialised.TransferListItems;

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
