using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Helpers;

public class TransferListHelperTests
{
    private readonly StateModel _state = TestData.StateWithClubs(3, new DateOnly(2026, 10, 15), myClubId: 1);
    private readonly RecordingNotifications _notifications = new();
    private readonly PlayerHelper _playerHelper;
    private readonly TransferListHelper _helper;

    public TransferListHelperTests()
    {
        _playerHelper = new PlayerHelper(_state);
        _helper = new TransferListHelper(_state, _playerHelper, new ClubHelper(_state), _notifications);
        var tactics = new TacticHelper(_state, _playerHelper);
        _state.Clubs.ForEach(tactics.ResetTacticForClub);
    }

    private PlayerModel PlayerAt(int clubId) => _state.Players.First(p => p.Contract?.ClubId == clubId);

    [Fact]
    public void AddPlayerToTransferList_RecordsAskingPriceAndCurrentClub()
    {
        var player = PlayerAt(2);

        _helper.AddPlayerToTransferList(player.Id, 1_500_000);

        var item = Assert.Single(_state.TransferListItems);
        Assert.Equal((player.Id, 1_500_000, 2), (item.PlayerId, item.AskingPrice, item.ClubId));
        Assert.True(_helper.IsPlayerTransferListed(player.Id));
        Assert.Same(item, _helper.GetTransferListItemByPlayerId(player.Id));
    }

    [Fact]
    public void RemovePlayerFromTransferList_RemovesAllEntriesForPlayer()
    {
        var player = PlayerAt(2);
        _helper.AddPlayerToTransferList(player.Id, 1);
        _helper.AddPlayerToTransferList(player.Id, 2);

        _helper.RemovePlayerFromTransferList(player.Id);

        Assert.False(_helper.IsPlayerTransferListed(player.Id));
        Assert.Null(_helper.GetTransferListItemByPlayerId(player.Id));
    }

    [Fact]
    public void TransferContractedPlayer_MovesPlayerMoneyAndTacticSlot()
    {
        var player = PlayerAt(2);
        var seller = _state.Clubs.First(c => c.Id == 2);
        var buyer = _state.Clubs.First(c => c.Id == 3);
        var sellerBudget = seller.TransferBudget;
        var buyerBudget = buyer.TransferBudget;
        _helper.AddPlayerToTransferList(player.Id, 2_000_000);

        _helper.TransferContractedPlayerByPlayerIdAndClubId(player.Id, buyer.Id);

        Assert.Equal(buyer.Id, player.Contract!.ClubId);
        Assert.Equal(buyer.Name, player.Contract.ClubName);
        Assert.Equal(sellerBudget + 2_000_000, seller.TransferBudget);
        Assert.Equal(buyerBudget - 2_000_000, buyer.TransferBudget);
        Assert.DoesNotContain(seller.TacticSlots, s => s.PlayerId == player.Id);
        Assert.Contains(buyer.TacticSlots, s => s.TacticSlotType == TacticSlotType.RES && s.PlayerId == player.Id);
        Assert.False(_helper.IsPlayerTransferListed(player.Id));
    }

    [Fact]
    public void TransferContractedPlayer_GivesAFreeShirtNumberAndKeepsGoalkeeperNumbersForKeepers()
    {
        // Buyer already uses 2..11.
        var buyerPlayers = _state.Players.Where(p => p.Contract?.ClubId == 3).Take(10).ToList();
        for (var i = 0; i < buyerPlayers.Count; i++) buyerPlayers[i].ShirtNumber = i + 2;
        var outfielder = _state.Players.First(p => p.Contract?.ClubId == 2 && p.Position != "GK");
        var keeper = _state.Players.First(p => p.Contract?.ClubId == 2 && p.Position == "GK");
        _helper.AddPlayerToTransferList(outfielder.Id, 1);
        _helper.AddPlayerToTransferList(keeper.Id, 1);

        _helper.TransferContractedPlayerByPlayerIdAndClubId(outfielder.Id, 3);
        _helper.TransferContractedPlayerByPlayerIdAndClubId(keeper.Id, 3);

        Assert.Equal(14, outfielder.ShirtNumber); // 12 and 13 are reserved for keepers
        Assert.Equal(1, keeper.ShirtNumber);
    }

    [Theory]
    [InlineData(2026, 10, 15, 2, 2028)] // mid-season: current season ends 30 Jun 2027
    [InlineData(2027, 3, 1, 1, 2027)]
    [InlineData(2026, 7, 2, 3, 2029)] // July counts as the new season
    public void SignFreeAgent_ContractRunsToTheEndOfTheSeasonPlusYears(int y, int m, int d, int years, int expectedExpiryYear)
    {
        _state.Date = new DateOnly(y, m, d);
        var freeAgent = TestData.Player(5000, clubId: null);
        _state.Players.Add(freeAgent);

        _helper.SignFreeAgentByPlayerId(freeAgent.Id, years);

        Assert.Equal(1, freeAgent.Contract!.ClubId);
        Assert.Equal(new DateOnly(expectedExpiryYear, 6, 30), freeAgent.Contract.ExpiryDate);
        Assert.NotNull(freeAgent.ShirtNumber);
        Assert.Contains(_state.Clubs[0].TacticSlots, s => s.TacticSlotType == TacticSlotType.RES && s.PlayerId == freeAgent.Id);
    }

    [Fact]
    public void UpdateTransferList_KeepsMyListingsAndAddsUpToTwentyPlayersPerLeague()
    {
        _state.Competitions.Add(TestData.League(1, _state.Clubs));
        var mine = PlayerAt(1);
        _helper.AddPlayerToTransferList(mine.Id, 123);
        _state.TransferListItems.Add(new TransferListItemModel { PlayerId = PlayerAt(2).Id, ClubId = 2, AskingPrice = 1 }); // stale AI listing

        _helper.UpdateTransferList();

        Assert.Contains(_state.TransferListItems, i => i.PlayerId == mine.Id && i.AskingPrice == 123);
        var aiItems = _state.TransferListItems.Where(i => i.ClubId != 1).ToList();
        Assert.Equal(20, aiItems.Count);
        Assert.Equal(aiItems.Count, aiItems.Select(i => i.PlayerId).Distinct().Count());
        foreach (var item in aiItems)
        {
            var value = _playerHelper.GetTransferValue(_state.Players.First(p => p.Id == item.PlayerId));
            Assert.InRange(item.AskingPrice, (int)(value * 0.6), (int)(value * 0.9));
        }
    }

    [Fact]
    public void UpdateTransferList_NeverListsMyOtherPlayers()
    {
        _state.Competitions.Add(TestData.League(1, _state.Clubs));

        _helper.UpdateTransferList();

        Assert.DoesNotContain(_state.TransferListItems, i => i.ClubId == 1);
    }

    [Fact]
    public void ProcessAITransfers_WithEmptyListDoesNothing()
    {
        _helper.ProcessAITransfers();

        Assert.Empty(_notifications.Sent);
        Assert.Empty(_state.Notifications);
    }

    [Fact]
    public void ProcessAITransfers_EventuallyDropsListingsForPlayersWhoLeftTheirClub()
    {
        var released = PlayerAt(2);
        _helper.AddPlayerToTransferList(released.Id, 1);
        released.Contract = null;

        // The number of AI transfers per day is random (0-2), so give it a few days.
        for (var i = 0; i < 100 && _state.TransferListItems.Count > 0; i++)
            _helper.ProcessAITransfers();

        Assert.Empty(_state.TransferListItems);
    }

    [Fact]
    public void ProcessAITransfers_AIClubsSignSuitableFreeAgents()
    {
        // Squads average a rating of about 62, so a 62-rated free agent suits every AI club.
        var freeAgent = TestData.Player(5000, clubId: null, rating: 62);
        _state.Players.Add(freeAgent);

        for (var i = 0; i < 200 && freeAgent.Contract == null; i++)
            _helper.ProcessAITransfers();

        var club = _state.Clubs.Single(c => c.Id == freeAgent.Contract!.ClubId);
        Assert.NotEqual(1, club.Id); // never signed to my club behind my back
        Assert.InRange(freeAgent.Contract!.ExpiryDate.Year, 2027, 2029);
        Assert.NotNull(freeAgent.ShirtNumber);
        Assert.Contains(club.TacticSlots, s => s.TacticSlotType == TacticSlotType.RES && s.PlayerId == freeAgent.Id);
        Assert.Contains(_notifications.Sent, n => n.Message.Contains(freeAgent.Name));
    }

    [Fact]
    public void ProcessAITransfers_AIClubsIgnoreRetiringAndUnsuitableFreeAgents()
    {
        var retiring = TestData.Player(5000, clubId: null, rating: 62);
        retiring.BirthDate = "01/15/1990";
        var tooGood = TestData.Player(5001, clubId: null, rating: 90);
        _state.Players.AddRange([retiring, tooGood]);

        for (var i = 0; i < 200; i++)
            _helper.ProcessAITransfers();

        Assert.Null(retiring.Contract);
        Assert.Null(tooGood.Contract);
    }

    [Fact]
    public void ProcessAITransfers_BidsForMyListedPlayersBecomeActionableNotifications()
    {
        // Club 2 is rich and its squad average matches my player's rating, so it is a suitable buyer.
        var mine = PlayerAt(1);
        _state.Clubs.First(c => c.Id == 2).TransferBudget = int.MaxValue;
        _helper.AddPlayerToTransferList(mine.Id, 1_000);

        for (var i = 0; i < 200 && !_state.Notifications.Any(n => n.BidPlayerId == mine.Id); i++)
            _helper.ProcessAITransfers();

        var bid = _state.Notifications.First(n => n.BidPlayerId == mine.Id);
        Assert.True(bid.RequiresAction);
        Assert.Equal(1, mine.Contract!.ClubId); // my player is never sold without my say-so
        Assert.NotEqual(1, bid.BiddingClubId);
    }
}
