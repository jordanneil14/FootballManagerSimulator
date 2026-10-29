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
[Collection(SaveFilesCollection.Name)]
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
        Assert.Equal("✅ Bid accepted", bid.Response);
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
        Assert.Equal("❌ Bid rejected", bid.Response);
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
    public void StartMatch_RejectsAnInjuredStarter()
    {
        AddFixtureToday(MyClub, _state.Clubs[1]);
        _engine.AutoPickTeam();
        var starter = MyClub.TacticSlots[0].PlayerId!.Value;
        var player = _state.Players.First(p => p.Id == starter);
        player.InjuryEndDate = _state.Date.AddDays(7);
        player.InjuryDescription = "Hamstring injury";

        Assert.Contains("injured", _engine.StartMatch());
    }

    [Fact]
    public void StartMatch_RejectsAnInjuredSubstitute()
    {
        AddFixtureToday(MyClub, _state.Clubs[1]);
        _engine.AutoPickTeam();
        var sub = MyClub.TacticSlots.First(s => s.TacticSlotType == TacticSlotType.SUB && s.PlayerId != null).PlayerId!.Value;
        var player = _state.Players.First(p => p.Id == sub);
        player.InjuryEndDate = _state.Date.AddDays(7);
        player.InjuryDescription = "Minor knock";

        Assert.Contains("injured", _engine.StartMatch());
    }

    [Fact]
    public void IsPlayerInjured_ReturnsTrueForActiveInjuryAndFalseOtherwise()
    {
        var player = _state.Players.First(p => p.Contract?.ClubId == MyClub.Id);
        Assert.False(_engine.IsPlayerInjured(player.Id));

        player.InjuryEndDate = _state.Date.AddDays(10);
        Assert.True(_engine.IsPlayerInjured(player.Id));

        player.InjuryEndDate = _state.Date.AddDays(-1);
        Assert.False(_engine.IsPlayerInjured(player.Id));
    }

    [Fact]
    public void IsPlayerUnavailable_CombinesSuspensionAndInjury()
    {
        var player = _state.Players.First(p => p.Contract?.ClubId == MyClub.Id);
        Assert.False(_engine.IsPlayerUnavailable(player.Id));

        player.SuspensionMatches = 1;
        Assert.True(_engine.IsPlayerUnavailable(player.Id));

        player.SuspensionMatches = 0;
        player.InjuryEndDate = _state.Date.AddDays(5);
        Assert.True(_engine.IsPlayerUnavailable(player.Id));
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

    // ----- English Cup prize money -------------------------------------------------------

    private CupModel AddEnglishCup()
    {
        var cup = TestData.LeagueCup(Enumerable.Range(0, 7).Select(i => _state.Date.AddDays(i * 14)).ToArray());
        cup.Name = "English Cup";
        _state.Competitions.Add(cup);
        return cup;
    }

    private FixtureModel AddCupTieToday(CupModel cup, ClubModel winner, ClubModel loser, int round)
    {
        var tie = AddFixtureToday(winner, loser, cup);
        (tie.Round, tie.GoalsHome, tie.GoalsAway, tie.Concluded, tie.ClubWon) = (round, 1, 0, true, winner);
        return tie;
    }

    [Fact]
    public void CupTies_PayBothClubsOnceTheDaysResultsAreIn_TheWinnerDouble()
    {
        var (winner, loser) = (_state.Clubs[1], _state.Clubs[2]);
        var (winnerBudget, loserBudget) = (winner.TransferBudget, loser.TransferBudget);
        AddCupTieToday(AddEnglishCup(), winner, loser, round: 3);

        _engine.AdvanceTodaysFixtures();

        Assert.Equal(winnerBudget + CupPrizeMoneyRules.ForWinner(3), winner.TransferBudget);
        Assert.Equal(loserBudget + CupPrizeMoneyRules.ForLoser(3), loser.TransferBudget);
        Assert.Equal(2 * CupPrizeMoneyRules.ForLoser(3), CupPrizeMoneyRules.ForWinner(3));
        Assert.DoesNotContain(_state.Notifications, n => n.Subject.EndsWith("Prize Money")); // not my tie
    }

    [Fact]
    public void CupTies_PayNothingWhileMyMatchIsBeingPlayed_ThenPayAtFullTime()
    {
        var cup = AddEnglishCup();
        var opponent = _state.Clubs[1];
        var budget = MyClub.TransferBudget;
        var otherTie = AddCupTieToday(cup, _state.Clubs[2], TestData.Club(77), round: 7);
        var mine = AddFixtureToday(MyClub, opponent, cup);
        mine.Round = 7;
        var otherWinnerBudget = _state.Clubs[2].TransferBudget;

        _engine.Advance(); // my match isn't over, so nobody is paid yet
        Assert.False(otherTie.PrizeMoneyPaid);
        Assert.Equal(otherWinnerBudget, _state.Clubs[2].TransferBudget);

        (mine.GoalsHome, mine.GoalsAway, mine.Concluded, mine.ClubWon) = (2, 1, true, MyClub);
        _engine.CompleteRemainingFixtures();
        _engine.CompleteRemainingFixtures(); // a second pass must not pay again

        Assert.Equal(budget + CupPrizeMoneyRules.ForWinner(7), MyClub.TransferBudget);
        Assert.Equal(otherWinnerBudget + CupPrizeMoneyRules.ForWinner(7), _state.Clubs[2].TransferBudget);
        var notification = Assert.Single(_state.Notifications, n => n.Subject == "English Cup Prize Money");
        Assert.Contains($"win over {opponent.Name} in the final of the English Cup", notification.Message);
        Assert.Contains($"£{CupPrizeMoneyRules.ForWinner(7):n0}", notification.Message);
    }

    [Fact]
    public void CupTies_TellMeWhatALossStillEarned()
    {
        AddCupTieToday(AddEnglishCup(), _state.Clubs[1], MyClub, round: 5);
        var budget = MyClub.TransferBudget;

        _engine.CompleteRemainingFixtures();

        Assert.Equal(budget + CupPrizeMoneyRules.ForLoser(5), MyClub.TransferBudget);
        Assert.Contains("defeat to Club 2 in the quarter-final", Assert.Single(_state.Notifications).Message);
    }

    [Fact]
    public void CupTies_PayTheRealClubsAfterALoadNotTheFixtureCopies()
    {
        var copy = TestData.Club(2, budget: 0);
        AddCupTieToday(AddEnglishCup(), copy, TestData.Club(3, budget: 0), round: 1);
        var budget = _state.Clubs[1].TransferBudget;

        _engine.CompleteRemainingFixtures();

        Assert.Equal(budget + CupPrizeMoneyRules.ForWinner(1), _state.Clubs[1].TransferBudget);
        Assert.Equal(0, copy.TransferBudget);
    }

    [Fact]
    public void LeagueWins_EarnNoPrizeMoney()
    {
        var fixture = AddFixtureToday(_state.Clubs[1], _state.Clubs[2]);
        (fixture.GoalsHome, fixture.GoalsAway, fixture.Concluded, fixture.ClubWon) = (1, 0, true, _state.Clubs[1]);
        var budget = _state.Clubs[1].TransferBudget;

        _engine.CompleteRemainingFixtures();

        Assert.Equal(budget, _state.Clubs[1].TransferBudget);
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

    // ----- In-match substitutions ---------------------------------------------------------

    private (FixtureModel Fixture, List<int> Pitch, List<int> Bench) LiveMatch()
    {
        _engine.AutoPickTeam();
        var slots = MyClub.TacticSlots;
        var pitch = slots.Select((s, i) => (s, i)).Where(t => t.s.TacticSlotType is not (TacticSlotType.SUB or TacticSlotType.RES)).Select(t => t.i).ToList();
        var bench = slots.Select((s, i) => (s, i)).Where(t => t.s.TacticSlotType == TacticSlotType.SUB && t.s.PlayerId != null).Select(t => t.i).ToList();
        return (new FixtureModel { HomeClub = MyClub, AwayClub = _state.Clubs[1] }, pitch, bench);
    }

    private static readonly LeagueModel League = new() { Type = CompetitionType.League };

    [Fact]
    public void MatchSwap_AllowsFiveSubstitutionsInACompetitiveMatchButNotASixth()
    {
        var (fixture, pitch, bench) = LiveMatch();
        Assert.True(bench.Count > GameEngine.MaxSubstitutions);

        for (var i = 0; i < GameEngine.MaxSubstitutions; i++)
            Assert.Null(_engine.MatchSwap(fixture, League, pitch[i + 1], bench[i], 60));
        var unused = MyClub.TacticSlots[bench[5]].PlayerId;

        Assert.Contains("all 5 substitutions", _engine.MatchSwap(fixture, League, pitch[7], bench[5], 70));

        Assert.Equal(GameEngine.MaxSubstitutions, _engine.SubstitutionsMade(fixture));
        Assert.Equal(unused, MyClub.TacticSlots[bench[5]].PlayerId); // the sixth swap was not made
    }

    [Fact]
    public void MatchSwap_APlayerTakenOffCannotComeBackOn()
    {
        var (fixture, pitch, bench) = LiveMatch();
        Assert.Null(_engine.MatchSwap(fixture, League, pitch[1], bench[0], 30));

        // The player taken off now sits in the bench slot; bring him on for someone else later.
        var error = _engine.MatchSwap(fixture, League, pitch[2], bench[0], 50);

        Assert.Contains("cannot come back on", error);
        Assert.Equal(1, _engine.SubstitutionsMade(fixture));
    }

    [Fact]
    public void MatchSwap_SwappingTheSamePairBackAtTheSameMinuteUndoesTheSubstitution()
    {
        var (fixture, pitch, bench) = LiveMatch();
        var starter = MyClub.TacticSlots[pitch[1]].PlayerId;

        Assert.Null(_engine.MatchSwap(fixture, League, pitch[1], bench[0], 30));
        Assert.Null(_engine.MatchSwap(fixture, League, pitch[1], bench[0], 30));

        Assert.Equal(starter, MyClub.TacticSlots[pitch[1]].PlayerId);
        Assert.Equal(0, _engine.SubstitutionsMade(fixture));
    }

    [Fact]
    public void MatchSwap_MovingPlayersAroundThePitchIsNotASubstitution()
    {
        var (fixture, pitch, _) = LiveMatch();

        for (var i = 0; i < 8; i++)
            Assert.Null(_engine.MatchSwap(fixture, League, pitch[1], pitch[2], 40));

        Assert.Equal(0, _engine.SubstitutionsMade(fixture));
    }

    [Fact]
    public void MatchSwap_FriendliesHaveNoSubstitutionLimit()
    {
        var (fixture, pitch, bench) = LiveMatch();
        var friendly = new FriendlyModel();

        for (var i = 0; i < bench.Count; i++)
            Assert.Null(_engine.MatchSwap(fixture, friendly, pitch[i + 1], bench[i], 60));
        Assert.Null(_engine.MatchSwap(fixture, friendly, pitch[1], bench[0], 70)); // and players can return
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
    public void SubmitBid_ForAListedPlayerNeedsAtLeastTheAskingPrice()
    {
        var target = PlayerAt(2);
        var askingPrice = (int)(_players.GetTransferValue(target) * 1.5);
        _transfers.AddPlayerToTransferList(target.Id, askingPrice);

        Assert.True(_engine.SubmitBid(target.Id, askingPrice).Accepted);
        var counter = _engine.SubmitBid(target.Id, askingPrice - 1);
        Assert.False(counter.Accepted);
        Assert.Equal(askingPrice, counter.CounterOffer);
        Assert.Contains("outright", _engine.SubmitBid(target.Id, (int)(askingPrice * 0.4)).Message);
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
        Assert.Equal("Insufficient budget.", _engine.SubmitBid(target.Id, MyClub.TransferBudget + 1).Message);
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
    public void MySquadAverageRating_CountsOnlyMyBest20Players()
    {
        _state.Players.Where(p => p.Contract?.ClubId == MyClub.Id).ToList().ForEach(p => p.Rating = 40);
        for (var id = 6000; id < 6000 + GameEngine.SquadAverageBestPlayers; id++)
        {
            var player = TestData.Player(id, clubId: MyClub.Id);
            player.Rating = 70;
            _state.Players.Add(player);
        }

        Assert.Equal(70, _engine.MySquadAverageRating());

        var freeAgent = TestData.Player(5000, clubId: null);
        freeAgent.Rating = 75;
        Assert.Null(_engine.SigningLevelRefusal(freeAgent));
    }

    [Theory]
    [InlineData(-30, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void BuyTransferListedPlayer_RefusesPlayersMoreThan5AboveMySquadAverage(int offset, bool signs)
    {
        _state.Players.Where(p => p.Contract?.ClubId == MyClub.Id).ToList().ForEach(p => p.Rating = 70);
        var target = PlayerAt(2);
        target.Rating = 70 + offset;
        _transfers.AddPlayerToTransferList(target.Id, 100);

        var message = _engine.BuyTransferListedPlayer(target.Id);

        if (signs)
        {
            Assert.Equal(MyClub.Id, target.Contract!.ClubId);
        }
        else
        {
            Assert.Contains("turned down", message);
            Assert.Equal(2, target.Contract!.ClubId);
            Assert.Contains("turned down", _engine.SubmitBid(target.Id, 100).Message);
            Assert.Contains("turned down", _engine.CompletePurchase(target.Id, 100, 2));
            Assert.Equal(2, target.Contract.ClubId);
        }
    }

    [Fact]
    public void SigningLevelRefusal_AppliesToUnlistedContractedPlayers()
    {
        _state.Players.Where(p => p.Contract?.ClubId == MyClub.Id).ToList().ForEach(p => p.Rating = 50);
        var target = PlayerAt(2);
        target.Rating = 90;

        Assert.NotNull(_engine.SigningLevelRefusal(target));
        Assert.Contains("turned down", _engine.SubmitBid(target.Id, 100).Message);
        Assert.Contains("turned down", _engine.CompletePurchase(target.Id, 100, 2));
        Assert.Equal(2, target.Contract!.ClubId);
    }

    [Fact]
    public void SigningLevelRefusal_NeverAppliesToMyOwnPlayers()
    {
        _state.Players.Where(p => p.Contract?.ClubId == MyClub.Id).ToList().ForEach(p => p.Rating = 50);
        var mine = PlayerAt(1);
        mine.Rating = 90;

        Assert.Null(_engine.SigningLevelRefusal(mine));
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
    public void SaveGame_IsRefusedOnlyFromKickOffUntilTheDaysResultsAreIn()
    {
        AddFixtureToday(MyClub, _state.Clubs[1]);
        _engine.AutoPickTeam();

        // A match day before kick-off can still be saved.
        Assert.True(_engine.CanSave);
        Assert.Equal("Game saved successfully.", _engine.SaveGame("before-kick-off"));

        Assert.Null(_engine.StartMatch());

        Assert.False(_engine.CanSave);
        Assert.Equal(GameEngine.SaveBlockedDuringMatchMessage, _engine.SaveGame("mid-match"));
        Assert.DoesNotContain(_engine.ListSaves(), s => s.FileName == "mid-match.fms");

        _engine.CompleteRemainingFixtures();

        Assert.True(_engine.CanSave);
        Assert.Equal("Game saved successfully.", _engine.SaveGame("after-match"));
    }

    [Fact]
    public void SaveGame_IsAllowedOnceMyMatchHasFinished_EvenWithoutPressingContinue()
    {
        var fixture = AddFixtureToday(MyClub, _state.Clubs[1]);
        _engine.AutoPickTeam();
        Assert.Null(_engine.StartMatch());

        fixture.Concluded = true; // full time, then left the match another way than Continue

        Assert.True(_engine.CanSave);
        Assert.Equal("Game saved successfully.", _engine.SaveGame("after-leaving"));
    }

    [Fact]
    public void Advance_ClearsAMatchInProgressFlagLeftFromThePreviousDay()
    {
        AddFixtureToday(MyClub, _state.Clubs[1]);
        _engine.AutoPickTeam();
        Assert.Null(_engine.StartMatch());

        _engine.Advance();

        Assert.False(_state.MatchInProgress);
        Assert.True(_engine.CanSave);
    }

    [Fact]
    public void ListSaves_SkipsCorruptFiles()
    {
        File.WriteAllText(Path.Combine(_storage.SaveDirectory, "broken.fms"), "{ not json");
        _engine.SaveGame("good");

        Assert.Contains(_engine.ListSaves(), s => s.FileName == "good.fms");
    }

    [Fact]
    public void MostRecentSave_IsTheLastWrittenSave_ManualOrAutosave()
    {
        _state.ManagerName = "Tester";
        _engine.SaveGame("older");
        _engine.SaveGame("newer");
        File.SetLastWriteTime(Path.Combine(_storage.SaveDirectory, "older.fms"), DateTime.Now.AddHours(-2));
        File.SetLastWriteTime(Path.Combine(_storage.SaveDirectory, "newer.fms"), DateTime.Now.AddHours(-1));

        Assert.True(_engine.AutoSave());

        var latest = _engine.MostRecentSave();
        Assert.Equal((_engine.AutosaveFileName, "Club 1", "Tester", true), (latest!.FileName, latest.ClubName, latest.ManagerName, latest.IsAutosave));
    }

    [Fact]
    public void MostRecentSave_SkipsAnUnreadableNewestFile()
    {
        _engine.SaveGame("good");
        File.SetLastWriteTime(Path.Combine(_storage.SaveDirectory, "good.fms"), DateTime.Now.AddHours(-1));
        File.WriteAllText(Path.Combine(_storage.SaveDirectory, "broken.fms"), "{ not json");

        Assert.Equal("good.fms", _engine.MostRecentSave()!.FileName);
    }

    [Fact]
    public void MostRecentSave_IsNullWithNoSaves()
    {
        Assert.Null(_engine.MostRecentSave());
    }

    [Fact]
    public void MostRecentSave_CanBeLoadedStraightBack()
    {
        _state.ManagerName = "Tester";
        _engine.SaveGame("career");
        var date = _state.Date;
        _state.Date = date.AddDays(10);

        Assert.Null(_engine.LoadGame(_engine.MostRecentSave()!.FileName));

        Assert.Equal(date, _state.Date);
        Assert.True(_engine.GameInProgress);
    }

    [Fact]
    public void DeleteSave_RemovesOnlyThatSave()
    {
        _engine.SaveGame("keep");
        _engine.SaveGame("remove");

        Assert.Null(_engine.DeleteSave("remove.fms"));

        Assert.Equal(["keep.fms"], _engine.ListSaves().Select(s => s.FileName));
        Assert.NotNull(_engine.DeleteSave("remove.fms")); // already gone
    }

    [Theory]
    [InlineData("..\\outside.fms")]
    [InlineData("../outside.fms")]
    [InlineData("notes.txt")]
    public void DeleteSave_RefusesAnythingButASaveInTheSavesFolder(string fileName)
    {
        var outside = Path.Combine(Path.GetDirectoryName(_storage.SaveDirectory)!, "outside.fms");
        File.WriteAllText(outside, "{}");
        File.WriteAllText(Path.Combine(_storage.SaveDirectory, "notes.txt"), "");

        Assert.NotNull(_engine.DeleteSave(fileName));

        Assert.True(File.Exists(outside));
        Assert.True(File.Exists(Path.Combine(_storage.SaveDirectory, "notes.txt")));
        File.Delete(outside);
    }

    [Fact]
    public void LoadGame_ReportsAMissingFile() =>
        Assert.NotNull(_engine.LoadGame("does-not-exist.fms"));

    [Fact]
    public void LoadGame_ReadsOlderSavesThatHoldFullClubCopiesInFixtures()
    {
        var league = TestData.League(1, _state.Clubs);
        _state.Competitions.Add(league);
        league.Fixtures.Add(new FixtureModel { HomeClub = MyClub, AwayClub = _state.Clubs[1], ClubWon = MyClub, Concluded = true, GoalsHome = 2, GoalsAway = 1 });

        // The old format: every club serialized in full wherever it appears.
        var oldJson = Newtonsoft.Json.JsonConvert.SerializeObject(_state,
            new Newtonsoft.Json.JsonSerializerSettings { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto });
        File.WriteAllText(Path.Combine(_storage.SaveDirectory, "old.fms"), oldJson);

        Assert.Null(_engine.LoadGame("old.fms"));

        var fixture = Assert.Single(_state.Competitions.Single().Fixtures);
        Assert.Same(_state.Clubs[0], fixture.HomeClub);
        Assert.Same(_state.Clubs[1], fixture.AwayClub);
        Assert.Same(_state.Clubs[0], fixture.ClubWon);
        Assert.Equal(_state.Clubs, _state.Competitions.Single().Clubs);
        Assert.Equal(MyClub.TacticSlots.Count, fixture.HomeClub.TacticSlots.Count);
    }

    [Fact]
    public void SaveGame_LeavesOutTheScreenStack_AndOlderSavesThatHaveOneStillLoad()
    {
        _state.ScreenStack.Push(new ScreenModel { Type = default });
        _engine.SaveGame("screens");
        Assert.DoesNotContain("ScreenStack", File.ReadAllText(Path.Combine(_storage.SaveDirectory, "screens.fms")));

        // Older saves wrote the stack out; release builds could not read it back.
        var oldJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { _state.Date, _state.ManagerName, ScreenStack = new[] { new ScreenModel() } });
        File.WriteAllText(Path.Combine(_storage.SaveDirectory, "old-screens.fms"), oldJson);

        Assert.Null(_engine.LoadGame("old-screens.fms"));
        Assert.Empty(_state.ScreenStack);
    }

    [Fact]
    public void LoadGame_RestoresTheCareerIdAndSettings()
    {
        _state.CareerId = "career1";
        _state.MatchSpeedMs = 250;
        _state.AutoSaveEnabled = false;
        _state.ShowTransferNotifications = false;
        _state.ShowManagerNotifications = false;
        _engine.SaveGame("settings");
        (_state.CareerId, _state.MatchSpeedMs, _state.AutoSaveEnabled) = ("other", 1000, true);
        (_state.ShowTransferNotifications, _state.ShowManagerNotifications) = (true, true);

        Assert.Null(_engine.LoadGame("settings.fms"));

        Assert.Equal(("career1", 250, false), (_state.CareerId, _state.MatchSpeedMs, _state.AutoSaveEnabled));
        Assert.False(_state.ShowTransferNotifications);
        Assert.False(_state.ShowManagerNotifications);
    }

    [Fact]
    public void LoadGame_RestoresThatCareersPhysios()
    {
        _state.Physios = [new PhysioModel { Id = 1, Name = "Sam Physio", HiredDate = _state.Date, MonthsWorked = 2 }];
        _engine.SaveGame("with-physio");
        _state.Physios = [new PhysioModel { Id = 1, Name = "Other Career" }, new PhysioModel { Id = 2, Name = "Other Career" }];

        Assert.Null(_engine.LoadGame("with-physio.fms"));

        var physio = Assert.Single(_state.Physios);
        Assert.Equal(("Sam Physio", 2), (physio.Name, physio.MonthsWorked));
    }

    [Fact]
    public void LoadGame_GivesSavesFromBeforeAutosaveTheirOwnCareerId()
    {
        _state.CareerId = "";
        _engine.SaveGame("before-autosave");

        Assert.Null(_engine.LoadGame("before-autosave.fms"));

        Assert.NotEmpty(_state.CareerId);
    }

    [Fact]
    public void StartNewGame_StartsANewCareer()
    {
        _state.CareerId = "old-career";

        _engine.StartNewGame();

        Assert.NotEmpty(_state.CareerId);
        Assert.NotEqual("old-career", _state.CareerId);
    }

    // ----- Autosave -----------------------------------------------------------------------

    private string AutosavePath => Path.Combine(_storage.SaveDirectory, _engine.AutosaveFileName);

    [Fact]
    public void Advance_AutosavesTheCareerToItsOwnSlotOnMondays()
    {
        _state.CareerId = "career1";
        _state.Date = new DateOnly(2026, 7, 21); // a Tuesday; the mocked day processing leaves the date alone
        _engine.Advance();
        Assert.False(File.Exists(AutosavePath));

        _state.Date = new DateOnly(2026, 7, 27);
        _engine.Advance();

        var save = Assert.Single(_engine.ListSaves());
        Assert.Equal("autosave-career1.fms", save.FileName);
        Assert.True(save.IsAutosave);
        Assert.Empty(Directory.GetFiles(_storage.SaveDirectory, "*.tmp"));
    }

    [Fact]
    public void Advance_DoesNotAutosaveWhenItCannotMoveOn()
    {
        Assert.Equal(GameEngine.AutosaveDay, _state.Date.DayOfWeek);

        _process.Setup(p => p.Process()).Throws(new ProcessException(ScreenType.Fixture));
        _engine.Advance();

        _process.Reset();
        _state.Notifications.Add(new NotificationModel { Date = _state.Date, RequiresAction = true });
        _engine.Advance();

        Assert.False(File.Exists(AutosavePath));
    }

    [Fact]
    public void CompleteRemainingFixtures_AutosavesOnceTheMatchDayIsOver()
    {
        Assert.True(_engine.AutoSave()); // nothing to play today
        File.Delete(AutosavePath);

        AddFixtureToday(MyClub, _state.Clubs[1]);
        _engine.AutoPickTeam();
        Assert.Null(_engine.StartMatch());

        // Mid-match: the user's fixture is still being played.
        Assert.False(_engine.AutoSave());

        _engine.CompleteRemainingFixtures();
        Assert.True(File.Exists(AutosavePath));
    }

    [Fact]
    public void AutoSave_IsSkippedWhenTurnedOffOrWithNoGame()
    {
        _state.AutoSaveEnabled = false;
        Assert.False(_engine.AutoSave());

        _state.AutoSaveEnabled = true;
        _state.MyClubId = null; // a game still being set up, with no job history
        Assert.False(_engine.AutoSave());

        Assert.Empty(Directory.GetFiles(_storage.SaveDirectory));
    }

    [Fact]
    public void AutoSave_FailingPartWayLeavesThePreviousAutosaveIntact()
    {
        _state.ManagerName = "Before";
        Assert.True(_engine.AutoSave());

        // Block the temporary file so the next write fails before the swap.
        Directory.CreateDirectory(AutosavePath + ".tmp");
        _state.ManagerName = "After";

        Assert.False(_engine.AutoSave());
        Assert.Contains(_state.UserFeedbackUpdates, m => m.StartsWith("Autosave failed"));

        Directory.Delete(AutosavePath + ".tmp");
        Assert.Null(_engine.LoadGame(_engine.AutosaveFileName));
        Assert.Equal("Before", _state.ManagerName);
    }
}
