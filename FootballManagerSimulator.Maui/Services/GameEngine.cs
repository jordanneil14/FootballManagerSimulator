using System.Globalization;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Exceptions;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

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

    /// <summary>
    /// Raised whenever the game state changes in a way that the persistent layout
    /// (top bar, side-nav) should reflect. Layout components subscribe to re-render.
    /// </summary>
    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();

    public void MarkLatestNotificationRead()
    {
        if (State.Notifications.Any())
            State.Notifications.RemoveAt(0);
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

    public enum AdvanceResult { Advanced, MustPlayFixtures }

    public AdvanceResult Advance()
    {
        try
        {
            processHelper.Process();
            return AdvanceResult.Advanced;
        }
        catch (ProcessException)
        {
            // Outstanding fixtures exist for the current day – the user must play them first.
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
    public string? StartMatch()
    {
        var unfilledStarters = MyClub.TacticSlots
            .Where(p => p.TacticSlotType != TacticSlotType.SUB && p.TacticSlotType != TacticSlotType.RES)
            .Any(p => p.PlayerId == null);

        if (unfilledStarters)
            return "Unable to start game. Your team has not been fully selected.";

        foreach (var competition in State.Competitions)
            foreach (var fixture in competition.Fixtures.Where(p => p.Date == State.Date))
                matchSimulator.ProcessMatch(fixture, competition);

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

    /// <summary>Reproduces the old FullTime screen "Continue" action.</summary>
    public void CompleteRemainingFixtures()
    {
        foreach (var competition in State.Competitions)
            foreach (var fixture in competition.Fixtures.Where(p => p.Date == State.Date && p.ClubWon == null))
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
        MyClub.Formation = formation;
        tacticHelper.ResetTacticForClub(MyClub);
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

    public string? BuyTransferListedPlayer(int playerId)
    {
        if (!IsTransferWindowOpen)
            return "The transfer window is currently closed.";

        var item = transferListHelper.GetTransferListItemByPlayerId(playerId);
        if (item == null) return "This player is not on the transfer list.";
        if (item.AskingPrice > MyClub.TransferBudget) return "Insufficient funds to purchase this player.";

        transferListHelper.TransferContractedPlayerByPlayerIdAndClubId(item.PlayerId, MyClub.Id);
        var player = playerHelper.GetPlayerById(playerId);
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

    public string SignFreeAgent(int playerId)
    {
        transferListHelper.SignFreeAgentByPlayerId(playerId);
        var player = playerHelper.GetPlayerById(playerId);
        return player == null ? "Player signed." : $"{player.Name} has been signed.";
    }

    // ---------------------------------------------------------------------------------
    // Finances
    // ---------------------------------------------------------------------------------

    public string RequestHigherTransferBudget()
    {
        var request = ActivatorUtilities.CreateInstance<IncreaseTransferBudgetRequestEvent>(serviceProvider, State.Date.AddDays(2));
        eventManager.ValidateAndAddEvent(request);
        return "Transfer budget request has been submitted.";
    }

    public string RequestStadiumExpansion()
    {
        var request = ActivatorUtilities.CreateInstance<StadiumExpansionRequestEvent>(serviceProvider, State.Date.AddDays(2));
        eventManager.ValidateAndAddEvent(request);
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
                var deserialised = JsonConvert.DeserializeObject<StateModel>(
                    File.ReadAllText(file.FullName),
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
                if (deserialised == null) continue;

                var myClub = deserialised.Clubs.FirstOrDefault(p => p.Id == deserialised.MyClubId);
                games.Add(new LoadGamePreviewModel
                {
                    FileName = file.Name,
                    ClubName = myClub?.Name ?? "",
                    ManagerName = deserialised.ManagerName,
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

    public string? LoadGame(string fileName)
    {
        try
        {
            var fileContent = File.ReadAllText(Path.Combine(SaveDirectory, fileName));
            var deserialised = JsonConvert.DeserializeObject<StateModel>(fileContent, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
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
            foreach (var @event in deserialised.Events)
            {
                var gameEvent = (IEvent)ActivatorUtilities.CreateInstance(
                    serviceProvider,
                    @event.GetType(),
                    @event.TriggerDate,
                    @event.IsCompleted);
                eventManager.ValidateAndAddEvent(gameEvent);
            }

            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
