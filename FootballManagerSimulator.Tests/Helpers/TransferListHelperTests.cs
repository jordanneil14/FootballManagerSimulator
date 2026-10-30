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

        Assert.Equal(12, outfielder.ShirtNumber); // his line's numbers are taken, so the lowest unreserved one
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
    public void AIFreeAgentSignings_AIClubsSignSuitableFreeAgents()
    {
        // Squads average a rating of about 62, so a 62-rated free agent suits every AI club.
        var freeAgent = TestData.Player(5000, clubId: null, rating: 62);
        _state.Players.Add(freeAgent);

        _helper.ProcessAIFreeAgentSignings();

        var club = _state.Clubs.Single(c => c.Id == freeAgent.Contract!.ClubId);
        Assert.NotEqual(1, club.Id); // never signed to my club behind my back
        Assert.InRange(freeAgent.Contract!.ExpiryDate.Year, 2027, 2029);
        Assert.NotNull(freeAgent.ShirtNumber);
        Assert.Contains(club.TacticSlots, s => s.TacticSlotType == TacticSlotType.RES && s.PlayerId == freeAgent.Id);
        Assert.Contains(_notifications.Sent, n => n.Message.Contains(freeAgent.Name));
    }

    [Fact]
    public void AIFreeAgentSignings_AIClubsWithFullSquadsSignNoFreeAgents()
    {
        // Fill both AI squads to the 30-player cap.
        foreach (var clubId in new[] { 2, 3 })
            for (var i = 0; i < 12; i++)
                _state.Players.Add(TestData.Player(1000 * clubId + i, clubId, rating: 62));
        var freeAgent = TestData.Player(5000, clubId: null, rating: 62);
        _state.Players.Add(freeAgent);

        _helper.ProcessAIFreeAgentSignings();

        Assert.Null(freeAgent.Contract);
    }

    [Fact]
    public void ProcessAITransfers_NoLongerSignsFreeAgents()
    {
        var freeAgent = TestData.Player(5000, clubId: null, rating: 62);
        _state.Players.Add(freeAgent);
        _helper.AddPlayerToTransferList(PlayerAt(2).Id, 1);

        for (var i = 0; i < 50; i++)
            _helper.ProcessAITransfers();

        Assert.Null(freeAgent.Contract);
    }

    [Fact]
    public void ProcessAITransfers_AIClubsWithFullSquadsDoNotBuyFromTheTransferList()
    {
        // Club 2 lists a player; club 3, the only other AI club, already has a full squad.
        for (var i = 0; i < 12; i++)
            _state.Players.Add(TestData.Player(3000 + i, clubId: 3, rating: 62));
        var listed = PlayerAt(2);
        _helper.AddPlayerToTransferList(listed.Id, 1);

        for (var i = 0; i < 200; i++)
            _helper.ProcessAITransfers();

        Assert.Equal(2, listed.Contract!.ClubId);
    }

    [Fact]
    public void UpdateTransferList_NeverListsRetiringPlayers()
    {
        _state.Competitions.Add(TestData.League(1, _state.Clubs));
        var retiring = _state.Players.Where(p => p.Contract?.ClubId != 1).Where((_, i) => i % 2 == 0).ToList();
        retiring.ForEach(p => p.BirthDate = "01/15/1990");

        _helper.UpdateTransferList();

        Assert.NotEmpty(_state.TransferListItems);
        Assert.DoesNotContain(_state.TransferListItems, i => retiring.Any(p => p.Id == i.PlayerId));
    }

    [Fact]
    public void AIFreeAgentSignings_AIClubsIgnoreRetiringAndTooWeakFreeAgents()
    {
        // Squads are about 62, so a 50 is more than 8 below even the short squads.
        var retiring = TestData.Player(5000, clubId: null, rating: 62);
        retiring.BirthDate = "01/15/1990";
        var tooWeak = TestData.Player(5001, clubId: null, rating: 50);
        _state.Players.AddRange([retiring, tooWeak]);

        _helper.ProcessAIFreeAgentSignings();

        Assert.Null(retiring.Contract);
        Assert.Null(tooWeak.Contract);
    }

    [Fact]
    public void AIFreeAgentSignings_APlayerTooGoodForEveryClubJoinsTheStrongestWithRoom()
    {
        // Club 3 is the strongest (about 66) but has a full squad; club 2 (about 63) has room. Both are far below a 94.
        foreach (var player in _state.Players.Where(p => p.Contract?.ClubId == 3))
            player.Rating += 4;
        for (var i = 0; i < 12; i++)
            _state.Players.Add(TestData.Player(3000 + i, clubId: 3, rating: 66));
        foreach (var player in _state.Players.Where(p => p.Contract?.ClubId == 2))
            player.Rating += 1;
        var star = TestData.Player(5000, clubId: null, rating: 94);
        _state.Players.Add(star);

        _helper.ProcessAIFreeAgentSignings();

        Assert.Equal(2, star.Contract!.ClubId);
    }

    [Fact]
    public void AIFreeAgentSignings_APlayerWithAClubAtHisLevelStillJoinsAClubAtHisLevel()
    {
        // Club 3 (about 66) is within 5 of a 70, so he joins it rather than the fallback.
        foreach (var player in _state.Players.Where(p => p.Contract?.ClubId == 3))
            player.Rating += 4;
        var freeAgent = TestData.Player(5000, clubId: null, rating: 70);
        _state.Players.Add(freeAgent);

        _helper.ProcessAIFreeAgentSignings();

        Assert.Equal(3, freeAgent.Contract!.ClubId);
    }

    [Fact]
    public void AIFreeAgentSignings_SignsUpToFiveFreeAgentsADayBestFirst()
    {
        // Squads' best 20 average about 62, so all eight (59-66) suit every AI club; only the best five sign today.
        var freeAgents = Enumerable.Range(0, 8).Select(i => TestData.Player(5000 + i, clubId: null, rating: 66 - i)).ToList();
        _state.Players.AddRange(freeAgents);

        _helper.ProcessAIFreeAgentSignings();

        Assert.All(freeAgents.Take(5), p => Assert.NotNull(p.Contract));
        Assert.All(freeAgents.Skip(5), p => Assert.Null(p.Contract));
    }

    [Fact]
    public void AIFreeAgentSignings_FreeAgentsJoinTheStrongestClubTheyWouldPlayFor()
    {
        // Club 3 is stronger than club 2 but still within the free agent's reach.
        foreach (var player in _state.Players.Where(p => p.Contract?.ClubId == 3))
            player.Rating += 4;
        var freeAgent = TestData.Player(5000, clubId: null, rating: 68);
        _state.Players.Add(freeAgent);

        _helper.ProcessAIFreeAgentSignings();

        Assert.Equal(3, freeAgent.Contract!.ClubId);
    }

    [Fact]
    public void AIFreeAgentSignings_ClubsShortOfTheTargetSizeGetFirstPick()
    {
        // Club 3 has 27 players and is stronger (about 65); club 2 has 18 (about 62). Both suit a 64.
        for (var i = 0; i < 9; i++)
            _state.Players.Add(TestData.Player(3000 + i, clubId: 3, rating: 66));
        var freeAgent = TestData.Player(5000, clubId: null, rating: 64);
        _state.Players.Add(freeAgent);

        _helper.ProcessAIFreeAgentSignings();

        Assert.Equal(2, freeAgent.Contract!.ClubId);
    }

    [Theory]
    [InlineData(0, true)]   // 18-man squads are short, so take a player 7 below their level
    [InlineData(8, false)]  // 26-man squads only take players within 5
    public void AIFreeAgentSignings_ShortSquadsTakePlayersFurtherBelowTheirLevel(int extraPlayers, bool signed)
    {
        foreach (var clubId in new[] { 2, 3 })
            for (var i = 0; i < extraPlayers; i++)
                _state.Players.Add(TestData.Player(1000 * clubId + i, clubId, rating: 62));
        var freeAgent = TestData.Player(5000, clubId: null, rating: 55);
        _state.Players.Add(freeAgent);

        _helper.ProcessAIFreeAgentSignings();

        Assert.Equal(signed, freeAgent.Contract != null);
    }

    [Fact]
    public void SignFreeAgentsForShortSquads_SignsKeepersAndPlayersUpToTheMinimumFromThePool()
    {
        // Club 2 is down to 15 players with one keeper; club 1 (mine) is short too but is left alone.
        foreach (var clubId in new[] { 1, 2 })
        {
            var squad = _state.Players.Where(p => p.Contract?.ClubId == clubId).ToList();
            _state.Players.Remove(squad.First(p => p.Position == "GK"));
            _state.Players.RemoveAll(p => squad.Where(s => s.Position != "GK").Take(2).Contains(p));
        }
        var keepers = Enumerable.Range(0, 2).Select(i => TestData.Player(5000 + i, clubId: null, "GK", rating: 60)).ToList();
        var outfield = Enumerable.Range(0, 4).Select(i => TestData.Player(5100 + i, clubId: null, rating: 63 - i)).ToList();
        var tooGood = TestData.Player(5200, clubId: null, rating: 90);
        _state.Players.AddRange([.. keepers, .. outfield, tooGood]);

        _helper.SignFreeAgentsForShortSquads();

        var club2 = _state.Players.Where(p => p.Contract?.ClubId == 2).ToList();
        Assert.Equal(SquadSizeRules.Minimum, club2.Count);
        Assert.Equal(2, club2.Count(p => p.Position == "GK"));
        Assert.Equal(15, _state.Players.Count(p => p.Contract?.ClubId == 1));
        Assert.Null(tooGood.Contract);
        // One keeper, then the two best outfielders, bring club 2 back to 18.
        Assert.Equal([2, 2, null, null], outfield.Select(p => p.Contract?.ClubId));
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

    [Fact]
    public void ProcessAITransfers_RarelyBidsForPlayersListedFarAboveTheirValue()
    {
        var mine = _state.Players.Where(p => p.Contract?.ClubId == 1).Take(2).ToList();
        var (fair, overpriced) = (mine[0], mine[1]);
        _state.Clubs.First(c => c.Id == 2).TransferBudget = int.MaxValue;
        _helper.AddPlayerToTransferList(fair.Id, _playerHelper.GetTransferValue(fair));
        _helper.AddPlayerToTransferList(overpriced.Id, _playerHelper.GetTransferValue(overpriced) * 5);

        for (var i = 0; i < 400; i++)
            _helper.ProcessAITransfers();

        var fairBids = _state.Notifications.Count(n => n.BidPlayerId == fair.Id);
        var overpricedBids = _state.Notifications.Count(n => n.BidPlayerId == overpriced.Id);
        Assert.True(fairBids > 100, $"fair bids: {fairBids}");
        Assert.True(overpricedBids * 10 < fairBids, $"fair {fairBids}, overpriced {overpricedBids}");
    }
}
