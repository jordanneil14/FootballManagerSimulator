using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Exceptions;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;
using FootballManagerSimulator.Tests.Support;
using Moq;

namespace FootballManagerSimulator.Tests.Services;

/// <summary>
/// GameEngine with the simulation collaborators mocked (process, match, game factory) and the
/// cheap state helpers real, so the tests focus on the engine's own flow and transfer rules.
/// </summary>
public class GameEngineTests : IDisposable
{
    private readonly StateModel _state = TestData.StateWithClubs(3, new DateOnly(2026, 7, 20), myClubId: 1);
    private readonly Mock<IGameFactory> _gameFactory = new();
    private readonly Mock<IProcessHelper> _process = new();
    private readonly Mock<IMatchSimulatorHelper> _match = new();
    private readonly Mock<IGameCreator> _creator = new();
    private readonly InMemoryAppStorage _storage = new();
    private readonly TacticHelper _tactics;
    private readonly PlayerHelper _players;
    private readonly TransferListHelper _transfers;
    private readonly GameEngine _engine;
    private int _changedCount;

    public GameEngineTests()
    {
        _creator.SetupAllProperties();
        _players = new PlayerHelper(_state);
        var clubs = new ClubHelper(_state);
        _tactics = new TacticHelper(_state, _players);
        _transfers = new TransferListHelper(_state, _players, clubs, new RecordingNotifications());
        _state.Clubs.ForEach(_tactics.ResetTacticForClub);

        _engine = new GameEngine(_state, _creator.Object, _gameFactory.Object, _process.Object, _match.Object,
            _tactics, _transfers, _players, clubs, Mock.Of<IEventManager>(), _storage, Mock.Of<IServiceProvider>());
        _engine.Changed += () => _changedCount++;
    }

    public void Dispose() => _storage.Dispose();

    private ClubModel MyClub => _state.Clubs[0];
    private PlayerModel PlayerAt(int clubId) => _state.Players.First(p => p.Contract?.ClubId == clubId);

    // ----- Advance --------------------------------------------------------------------

    [Fact]
    public void Advance_ProcessesTheDay()
    {
        Assert.Equal(GameEngine.AdvanceResult.Advanced, _engine.Advance());
        _process.Verify(p => p.Process(), Times.Once);
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public void Advance_ReportsOutstandingFixtures()
    {
        _process.Setup(p => p.Process()).Throws(new ProcessException(ScreenType.Fixture));
        Assert.Equal(GameEngine.AdvanceResult.MustPlayFixtures, _engine.Advance());
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public void Advance_IsBlockedByAnUnansweredBid()
    {
        _state.Notifications.Add(new NotificationModel { Date = _state.Date, RequiresAction = true });

        Assert.Equal(GameEngine.AdvanceResult.BlockedByNotification, _engine.Advance());
        _process.Verify(p => p.Process(), Times.Never);
    }

    [Fact]
    public void Advance_IgnoresActionableNotificationsDatedInTheFuture()
    {
        _state.Notifications.Add(new NotificationModel { Date = _state.Date.AddDays(1), RequiresAction = true });
        Assert.Equal(GameEngine.AdvanceResult.Advanced, _engine.Advance());
    }

    // ----- New game ---------------------------------------------------------------------

    [Fact]
    public void StartNewGame_ClearsTheOldGameAndInitialisesANewOne()
    {
        _state.Notifications.Add(new NotificationModel());
        _state.TransferListItems.Add(new TransferListItemModel());

        _engine.StartNewGame();

        Assert.Null(_state.MyClubId);
        Assert.Empty(_state.Clubs);
        Assert.Empty(_state.Players);
        Assert.Empty(_state.Notifications);
        Assert.Empty(_state.TransferListItems);
        _gameFactory.Verify(f => f.IntitialiseGameState(), Times.Once);
        Assert.False(_engine.GameInProgress);
    }

    [Theory]
    [InlineData("  sir alex FERGUSON ", "Sir Alex Ferguson")]
    [InlineData("pep", "Pep")]
    public void SetManagerName_TrimsAndTitleCases(string input, string expected)
    {
        _engine.SetManagerName(input);
        Assert.Equal(expected, _creator.Object.ManagerName);
    }

    [Fact]
    public void SelectClub_FinalisesTheGame()
    {
        _engine.SelectLeague(4);
        _engine.SelectClub(7);

        Assert.Equal((4, 7), (_creator.Object.LeagueId, _creator.Object.ClubId));
        _gameFactory.Verify(f => f.FinaliseGameState(), Times.Once);
    }

    // ----- Notifications ----------------------------------------------------------------

    [Fact]
    public void MarkLatestNotificationRead_RemovesInformationalButNotActionableNotifications()
    {
        _state.Notifications.Add(new NotificationModel { Subject = "info" });
        _state.Notifications.Add(new NotificationModel { Subject = "bid", RequiresAction = true });

        _engine.MarkLatestNotificationRead();
        _engine.MarkLatestNotificationRead();

        Assert.Equal("bid", Assert.Single(_state.Notifications).Subject);
    }

    [Fact]
    public void ClearUnreadNotifications_KeepsActionableAndFutureNotifications()
    {
        _state.Notifications.Add(new NotificationModel { Subject = "info", Date = _state.Date });
        _state.Notifications.Add(new NotificationModel { Subject = "bid", Date = _state.Date, RequiresAction = true });
        _state.Notifications.Add(new NotificationModel { Subject = "future", Date = _state.Date.AddDays(1) });

        _engine.ClearUnreadNotifications();

        Assert.Equal(["bid", "future"], _state.Notifications.Select(p => p.Subject));
    }

    [Fact]
    public void AcceptBid_SellsThePlayer()
    {
        var player = PlayerAt(1);
        var buyer = _state.Clubs[1];
        var (myBudget, buyerBudget) = (MyClub.TransferBudget, buyer.TransferBudget);
        _transfers.AddPlayerToTransferList(player.Id, 1);
        var bid = new NotificationModel { RequiresAction = true, BidPlayerId = player.Id, BidAmount = 3_000_000, BiddingClubId = buyer.Id };

        _engine.AcceptBid(bid);

        Assert.False(bid.RequiresAction);
        Assert.Equal(buyer.Id, player.Contract!.ClubId);
        Assert.Equal(new DateOnly(2028, 6, 30), player.Contract.ExpiryDate);
        Assert.Equal(myBudget + 3_000_000, MyClub.TransferBudget);
        Assert.Equal(buyerBudget - 3_000_000, buyer.TransferBudget);
        Assert.DoesNotContain(MyClub.TacticSlots, s => s.PlayerId == player.Id);
        Assert.False(_transfers.IsPlayerTransferListed(player.Id));
    }

    [Fact]
    public void AcceptBid_GivesThePlayerAFreeNumberAtTheBuyingClub()
    {
        var player = PlayerAt(1);
        var buyer = _state.Clubs[1];
        player.ShirtNumber = 9;
        PlayerAt(2).ShirtNumber = 9; // the buyers already have a number 9
        var bid = new NotificationModel { RequiresAction = true, BidPlayerId = player.Id, BidAmount = 1_000, BiddingClubId = buyer.Id };

        _engine.AcceptBid(bid);

        var teammatesNumbers = _state.Players
            .Where(p => p.Contract?.ClubId == buyer.Id && p.Id != player.Id)
            .Select(p => p.ShirtNumber);
        Assert.NotNull(player.ShirtNumber);
        Assert.DoesNotContain(player.ShirtNumber, teammatesNumbers);
    }

    [Fact]
    public void AcceptBid_ForAPlayerWhoAlreadyLeftIsVoid()
    {
        var player = PlayerAt(2);
        var budget = MyClub.TransferBudget;
        var bid = new NotificationModel { RequiresAction = true, BidPlayerId = player.Id, BidAmount = 1, BiddingClubId = 3 };

        _engine.AcceptBid(bid);

        Assert.False(bid.RequiresAction);
        Assert.Contains("no longer valid", bid.Message);
        Assert.Equal(2, player.Contract!.ClubId);
        Assert.Equal(budget, MyClub.TransferBudget);
    }

    [Fact]
    public void RejectBid_KeepsThePlayer()
    {
        var player = PlayerAt(1);
        var bid = new NotificationModel { RequiresAction = true, BidPlayerId = player.Id, BidAmount = 1, BiddingClubId = 2 };

        _engine.RejectBid(bid);

        Assert.False(bid.RequiresAction);
        Assert.Contains("rejected", bid.Message);
        Assert.Equal(1, player.Contract!.ClubId);
    }

    // ----- Match flow ---------------------------------------------------------------------

    private FixtureModel AddFixtureToday(ClubModel home, ClubModel away, ICompetition? competition = null)
    {
        competition ??= _state.Competitions.FirstOrDefault() ?? AddLeague();
        var fixture = new FixtureModel { HomeClub = home, AwayClub = away, Date = _state.Date };
        competition.Fixtures.Add(fixture);
        return fixture;

        ICompetition AddLeague()
        {
            var league = TestData.League(1, _state.Clubs);
            _state.Competitions.Add(league);
            return league;
        }
    }

    [Fact]
    public void AdvanceTodaysFixtures_WithMyMatchGoesToPreMatch()
    {
        AddFixtureToday(MyClub, _state.Clubs[1]);

        Assert.Equal(GameEngine.TodaysFixturesResult.PreMatch, _engine.AdvanceTodaysFixtures());
        _match.Verify(m => m.PrepareMatch(It.IsAny<FixtureModel>()), Times.Once);
        _match.Verify(m => m.ConcludeFixture(It.IsAny<FixtureModel>(), It.IsAny<ICompetition>()), Times.Never);
    }

    [Fact]
    public void AdvanceTodaysFixtures_WithoutMyMatchSimulatesEverything()
    {
        var other = AddFixtureToday(_state.Clubs[1], _state.Clubs[2]);

        Assert.Equal(GameEngine.TodaysFixturesResult.Results, _engine.AdvanceTodaysFixtures());
        _match.Verify(m => m.ConcludeFixture(other, It.IsAny<ICompetition>()), Times.Once);
    }

    [Fact]
    public void StartMatch_RequiresAFullStartingEleven()
    {
        AddFixtureToday(MyClub, _state.Clubs[1]);

        Assert.Contains("not been fully selected", _engine.StartMatch());
        _match.Verify(m => m.PreGenerateMatch(It.IsAny<FixtureModel>()), Times.Never);
    }

    [Fact]
    public void StartMatch_RejectsASuspendedStarter()
    {
        AddFixtureToday(MyClub, _state.Clubs[1]);
        _engine.AutoPickTeam();
        var starter = MyClub.TacticSlots[0].PlayerId!.Value;
        _state.Players.First(p => p.Id == starter).SuspensionMatches = 1;

        Assert.Contains("suspended", _engine.StartMatch());
    }

    [Fact]
    public void StartMatch_RejectsASuspendedSubstitute()
    {
        AddFixtureToday(MyClub, _state.Clubs[1]);
        _engine.AutoPickTeam();
        var sub = MyClub.TacticSlots.First(s => s.TacticSlotType == TacticSlotType.SUB && s.PlayerId != null).PlayerId!.Value;
        _state.Players.First(p => p.Id == sub).SuspensionMatches = 1;

        Assert.Contains("suspended", _engine.StartMatch());
    }

    [Fact]
    public void StartMatch_PreGeneratesMyMatchAndSimulatesTheRest()
    {
        var mine = AddFixtureToday(MyClub, _state.Clubs[1]);
        var others = AddFixtureToday(_state.Clubs[2], TestData.Club(77));
        _engine.AutoPickTeam();

        Assert.Null(_engine.StartMatch());

        _match.Verify(m => m.PreGenerateMatch(mine), Times.Once);
        _match.Verify(m => m.ConcludeFixture(others, It.IsAny<ICompetition>()), Times.Once);
        _match.Verify(m => m.ConcludeFixture(mine, It.IsAny<ICompetition>()), Times.Never);
    }

    [Fact]
    public void ContinueMatch_ReturnsTrueOnceMyFixtureHasConcluded()
    {
        var mine = AddFixtureToday(MyClub, _state.Clubs[1]);
        _match.Setup(m => m.ProcessMatch(mine, It.IsAny<ICompetition>())).Callback(() => mine.Minute += 45);

        Assert.False(_engine.ContinueMatch());
        mine.Concluded = true;
        Assert.True(_engine.ContinueMatch());
    }

    [Fact]
    public void NextMatchCaption_DescribesTheNextFixture()
    {
        Assert.Equal("Season Complete", _engine.NextMatchCaption());

        var league = TestData.League(1, _state.Clubs);
        _state.Competitions.Add(league);
        var fixture = new FixtureModel { HomeClub = _state.Clubs[1], AwayClub = MyClub, Date = _state.Date.AddDays(3) };
        league.Fixtures.Add(fixture);
        Assert.Equal("Next match: League 1 vs Club 2 in 3 days", _engine.NextMatchCaption());

        fixture.Date = _state.Date.AddDays(1);
        Assert.Equal("Next match: League 1 vs Club 2 tomorrow", _engine.NextMatchCaption());

        fixture.Date = _state.Date;
        Assert.Equal("Next match: League 1 vs Club 2 today", _engine.NextMatchCaption());

        (fixture.Concluded, fixture.GoalsHome, fixture.GoalsAway) = (true, 2, 1);
        Assert.Equal("Last match: Club 2 2 - 1 Club 1", _engine.NextMatchCaption());
    }

    // ----- Tactics ----------------------------------------------------------------------

    [Fact]
    public void SwapTacticSlots_SwapsPlayersAndIgnoresOutOfRangeIndexes()
    {
        _engine.AutoPickTeam();
        var (first, second) = (MyClub.TacticSlots[0].PlayerId, MyClub.TacticSlots[5].PlayerId);

        _engine.SwapTacticSlots(0, 5);
        _engine.SwapTacticSlots(-1, 5);
        _engine.SwapTacticSlots(0, 500);

        Assert.Equal((second, first), (MyClub.TacticSlots[0].PlayerId, MyClub.TacticSlots[5].PlayerId));
    }

    [Fact]
    public void ChangeFormation_KeepsPlayersInTheSameSlotOrder()
    {
        _engine.AutoPickTeam();
        var before = MyClub.TacticSlots.Select(s => s.PlayerId).ToList();

        _engine.ChangeFormation("4-3-3");

        Assert.Equal("4-3-3", MyClub.Formation);
        Assert.Equal(before, MyClub.TacticSlots.Select(s => s.PlayerId));
        Assert.Equal(TacticSlotType.RW, MyClub.TacticSlots[8].TacticSlotType);
    }

    // ----- Transfer window -------------------------------------------------------------

    [Theory]
    [InlineData(2026, 1, 15, true)]
    [InlineData(2026, 2, 1, false)]
    [InlineData(2026, 6, 14, false)]
    [InlineData(2026, 6, 15, true)]
    [InlineData(2026, 8, 31, true)]
    [InlineData(2026, 9, 1, false)]
    [InlineData(2026, 12, 31, false)]
    public void IsTransferWindowOpen(int y, int m, int d, bool open)
    {
        _state.Date = new DateOnly(y, m, d);
        Assert.Equal(open, _engine.IsTransferWindowOpen);
        Assert.Equal(open ? "Open" : "Closed", _engine.TransferWindowStatus);
    }

    [Theory]
    [InlineData(2026, 3, 10, 2026, 6, 15)]
    [InlineData(2026, 6, 1, 2026, 6, 15)]
    [InlineData(2026, 10, 1, 2027, 1, 1)]
    [InlineData(2026, 7, 4, 2026, 7, 4)] // already open
    public void NextTransferWindowDate(int y, int m, int d, int ey, int em, int ed)
    {
        _state.Date = new DateOnly(y, m, d);
        Assert.Equal(new DateOnly(ey, em, ed), _engine.NextTransferWindowDate);
    }

    // ----- Bids & purchases -------------------------------------------------------------

    [Fact]
    public void SubmitBid_ForAListedPlayerIsAcceptedAtNinetyPercentOfValue()
    {
        var target = PlayerAt(2);
        var value = _players.GetTransferValue(target);
        _transfers.AddPlayerToTransferList(target.Id, value);

        Assert.True(_engine.SubmitBid(target.Id, (int)(value * 0.9)).Accepted);
        var counter = _engine.SubmitBid(target.Id, (int)(value * 0.6));
        Assert.False(counter.Accepted);
        Assert.Equal(value, counter.CounterOffer);
    }

    [Fact]
    public void SubmitBid_ForAnUnlistedPlayerNeedsAPremium()
    {
        var target = PlayerAt(2);
        var value = _players.GetTransferValue(target);

        Assert.False(_engine.SubmitBid(target.Id, value).Accepted);
        Assert.Equal((int)(value * 1.3), _engine.SubmitBid(target.Id, value).CounterOffer);
        Assert.True(_engine.SubmitBid(target.Id, (int)(value * 1.3) + 1).Accepted);
    }

    [Fact]
    public void SubmitBid_TooLowIsRejectedOutright()
    {
        var target = PlayerAt(2);
        var result = _engine.SubmitBid(target.Id, (int)(_players.GetTransferValue(target) * 0.4));

        Assert.False(result.Accepted);
        Assert.Equal(0, result.CounterOffer);
        Assert.Contains("outright", result.Message);
    }

    [Fact]
    public void SubmitBid_ValidatesWindowBudgetAndPlayer()
    {
        var target = PlayerAt(2);
        Assert.Equal("Player not found.", _engine.SubmitBid(12345, 1).Message);
        Assert.Equal("Insufficient transfer budget.", _engine.SubmitBid(target.Id, MyClub.TransferBudget + 1).Message);
        _state.Date = new DateOnly(2026, 10, 1);
        Assert.Contains("closed", _engine.SubmitBid(target.Id, 1).Message);
    }

    [Fact]
    public void CompletePurchase_MovesThePlayerAndTheMoney()
    {
        var target = PlayerAt(2);
        var seller = _state.Clubs[1];
        var (myBudget, sellerBudget) = (MyClub.TransferBudget, seller.TransferBudget);
        _transfers.AddPlayerToTransferList(target.Id, 1);

        _engine.CompletePurchase(target.Id, 1_000_000, contractYears: 3);

        Assert.Equal(MyClub.Id, target.Contract!.ClubId);
        Assert.Equal(new DateOnly(2029, 6, 30), target.Contract.ExpiryDate);
        Assert.Equal(myBudget - 1_000_000, MyClub.TransferBudget);
        Assert.Equal(sellerBudget + 1_000_000, seller.TransferBudget);
        Assert.Contains(MyClub.TacticSlots, s => s.TacticSlotType == TacticSlotType.RES && s.PlayerId == target.Id);
        Assert.DoesNotContain(seller.TacticSlots, s => s.PlayerId == target.Id);
        Assert.False(_transfers.IsPlayerTransferListed(target.Id));
    }

    [Fact]
    public void BuyTransferListedPlayer_ChecksWindowListingAndFunds()
    {
        var target = PlayerAt(2);
        Assert.Contains("not on the transfer list", _engine.BuyTransferListedPlayer(target.Id));

        _transfers.AddPlayerToTransferList(target.Id, MyClub.TransferBudget + 1);
        Assert.Contains("Insufficient funds", _engine.BuyTransferListedPlayer(target.Id));

        _state.TransferListItems.Single().AskingPrice = 100;
        Assert.Contains("has been signed", _engine.BuyTransferListedPlayer(target.Id, contractYears: 1));
        Assert.Equal(MyClub.Id, target.Contract!.ClubId);
        Assert.Equal(new DateOnly(2027, 6, 30), target.Contract.ExpiryDate);

        _state.Date = new DateOnly(2026, 11, 1);
        Assert.Contains("closed", _engine.BuyTransferListedPlayer(PlayerAt(3).Id));
    }

    [Fact]
    public void RetiringPlayers_RefuseEveryNewContract()
    {
        // Turned 35 in January 2026, so retiring by July 2026.
        var target = PlayerAt(2);
        target.BirthDate = "01/15/1991";
        var freeAgent = TestData.Player(5000, clubId: null);
        freeAgent.BirthDate = "01/15/1991";
        _state.Players.Add(freeAgent);
        _transfers.AddPlayerToTransferList(target.Id, 100);

        Assert.Contains("retiring", _engine.SignFreeAgent(freeAgent.Id, 2));
        Assert.Null(freeAgent.Contract);
        Assert.Contains("retiring", _engine.SubmitBid(target.Id, MyClub.TransferBudget).Message);
        Assert.Contains("retiring", _engine.BuyTransferListedPlayer(target.Id));
        Assert.Contains("retiring", _engine.CompletePurchase(target.Id, 100, 2));
        Assert.Equal(2, target.Contract!.ClubId);
    }

    [Theory]
    [InlineData(-30, true)]
    [InlineData(-6, true)]
    [InlineData(0, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void SignFreeAgent_RefusesPlayersMoreThan5AboveMySquadAverage(int offset, bool signs)
    {
        _state.Players.Where(p => p.Contract?.ClubId == MyClub.Id).ToList().ForEach(p => p.Rating = 70);
        var freeAgent = TestData.Player(5000, clubId: null);
        freeAgent.Rating = 70 + offset;
        _state.Players.Add(freeAgent);

        var message = _engine.SignFreeAgent(freeAgent.Id, 2);

        if (signs)
        {
            Assert.Equal(MyClub.Id, freeAgent.Contract?.ClubId);
        }
        else
        {
            Assert.Contains("turned down", message);
            Assert.Null(freeAgent.Contract);
        }
    }

    [Fact]
    public void AcceptBid_FallsThroughOnceMyPlayerIsRetiring()
    {
        var mine = PlayerAt(1);
        mine.BirthDate = "01/15/1990";
        var budget = MyClub.TransferBudget;
        var bid = new NotificationModel { Date = _state.Date, RequiresAction = true, BidPlayerId = mine.Id, BidAmount = 500_000, BiddingClubId = 2 };
        _state.Notifications.Add(bid);

        _engine.AcceptBid(bid);

        Assert.Equal(1, mine.Contract!.ClubId);
        Assert.Equal(budget, MyClub.TransferBudget);
        Assert.False(bid.RequiresAction);
        Assert.Contains("fallen through", bid.Message);
    }

    [Fact]
    public void LoadGame_GivesClubsFromOlderSavesAStartingAcademyRating()
    {
        _state.Clubs.ForEach(c => c.AcademyRating = 0);
        _engine.SaveGame("before-academies");

        Assert.Null(_engine.LoadGame("before-academies.fms"));

        Assert.All(_state.Clubs, c => Assert.InRange(c.AcademyRating, AcademyRules.Min, AcademyRules.StartingMax));
    }

    [Fact]
    public void RenewContract_ExtendsFromTheEndOfTheCurrentSeason()
    {
        var mine = PlayerAt(1); // contracted until June 2028

        Assert.Contains("until", _engine.RenewContract(mine.Id, 4));
        Assert.Equal(new DateOnly(2030, 6, 30), mine.Contract!.ExpiryDate);
    }

    [Fact]
    public void RenewContract_MustRunPastTheCurrentDeal()
    {
        var mine = PlayerAt(1);

        Assert.Contains("already runs until", _engine.RenewContract(mine.Id, 2));
        Assert.Equal(new DateOnly(2028, 6, 30), mine.Contract!.ExpiryDate);
    }

    [Fact]
    public void RenewContract_IsRefusedByRetiringPlayersAndOtherClubsPlayers()
    {
        var mine = PlayerAt(1);
        mine.BirthDate = "01/15/1991";

        Assert.Contains("retiring", _engine.RenewContract(mine.Id, 5));
        Assert.Equal(new DateOnly(2028, 6, 30), mine.Contract!.ExpiryDate);
        Assert.Contains("not contracted to your club", _engine.RenewContract(PlayerAt(2).Id, 5));
    }

    [Fact]
    public void ListPlayerForTransfer_OnlyWhileTheWindowIsOpen()
    {
        var mine = PlayerAt(1);
        Assert.Null(_engine.ListPlayerForTransfer(mine.Id, 500));
        Assert.True(_engine.IsPlayerTransferListed(mine.Id));

        _state.Date = new DateOnly(2026, 10, 1);
        Assert.Contains("closed", _engine.ListPlayerForTransfer(PlayerAt(1).Id + 1, 500));
    }

    // ----- Save / load ------------------------------------------------------------------

    [Fact]
    public void SaveGame_WritesAnFmsFileThatListSavesCanPreview()
    {
        _state.ManagerName = "Tester";

        Assert.Equal("Game saved successfully.", _engine.SaveGame("slot1"));

        var preview = Assert.Single(_engine.ListSaves());
        Assert.Equal(("slot1.fms", "Club 1", "Tester"), (preview.FileName, preview.ClubName, preview.ManagerName));
    }

    [Fact]
    public void ListSaves_SkipsCorruptFiles()
    {
        File.WriteAllText(Path.Combine(_storage.SaveDirectory, "broken.fms"), "{ not json");
        _engine.SaveGame("good");

        Assert.Contains(_engine.ListSaves(), s => s.FileName == "good.fms");
    }

    [Fact]
    public void LoadGame_ReportsAMissingFile() =>
        Assert.NotNull(_engine.LoadGame("does-not-exist.fms"));
}
