using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Factories;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Helpers;

public class JobOfferHelperTests
{
    // Country 1: league 10 (rank 1) clubs 1-2, league 20 (rank 2) clubs 3-4, league 30 (rank 3) club 5.
    // Country 2: league 40 (rank 1) club 6. I manage club 3.
    private readonly StateModel _state = new() { Date = new DateOnly(2026, 9, 7), MyClubId = 3 };
    private readonly JobOfferHelper _helper;
    private readonly NotificationFactory _notifications;

    public JobOfferHelperTests()
    {
        AddLeague(10, countryId: 1, rank: 1, clubIds: [1, 2]);
        AddLeague(20, countryId: 1, rank: 2, clubIds: [3, 4]);
        AddLeague(30, countryId: 1, rank: 3, clubIds: [5]);
        AddLeague(40, countryId: 2, rank: 1, clubIds: [6]);
        _notifications = new NotificationFactory(_state);
        _helper = new JobOfferHelper(_state, _notifications);
    }

    private void AddLeague(int id, int countryId, int rank, int[] clubIds)
    {
        var clubs = clubIds.Select(c => TestData.Club(c, leagueId: id)).ToList();
        _state.Clubs.AddRange(clubs);
        var league = TestData.League(id, clubs);
        league.CountryId = countryId;
        league.Rank = rank;
        _state.Competitions.Add(league);
    }

    private NotificationModel PendingOffer() => _state.Notifications.Single(p => p.RequiresAction && p.JobOfferClubId != null);

    [Fact]
    public void TryMakeJobOffer_NothingUnlessSecure()
    {
        _state.BoardConfidence = BoardConfidenceRules.SecureFrom - 1;

        Assert.False(_helper.TryMakeJobOffer());
        Assert.Empty(_state.Notifications);
    }

    [Fact]
    public void TryMakeJobOffer_OnlyClubsInTheSameOrHigherLeagueOfMyCountry()
    {
        _state.BoardConfidence = BoardConfidenceRules.SecureFrom;
        var offeredBy = new HashSet<int>();

        for (var i = 0; i < 60; i++)
        {
            Assert.True(_helper.TryMakeJobOffer());
            var offer = PendingOffer();
            offeredBy.Add(offer.JobOfferClubId!.Value);
            _helper.DeclineJobOffer(offer);
        }

        Assert.Subset(new HashSet<int> { 1, 2, 4 }, offeredBy);
    }

    [Fact]
    public void TryMakeJobOffer_OnlyOneOfferAtATime()
    {
        _state.BoardConfidence = 90;

        Assert.True(_helper.TryMakeJobOffer());
        Assert.False(_helper.TryMakeJobOffer());
        Assert.Single(_state.Notifications);
    }

    [Fact]
    public void AcceptJobOffer_MovesMeToTheNewClubAndCancelsOldClubBusiness()
    {
        _state.BoardConfidence = 90;
        var budgetRequest = new IncreaseTransferBudgetRequestEvent(_state, _notifications, _state.Date.AddDays(2), false);
        _state.Events.Add(budgetRequest);
        var bid = new NotificationModel { Date = _state.Date, Subject = "Bid", RequiresAction = true, BidPlayerId = 1, BidAmount = 1, BiddingClubId = 4 };
        _state.Notifications.Add(bid);
        _helper.TryMakeJobOffer();
        var offer = PendingOffer();

        _helper.AcceptJobOffer(offer);

        Assert.Equal(offer.JobOfferClubId, _state.MyClubId);
        Assert.Equal(BoardConfidenceRules.Starting, _state.BoardConfidence);
        Assert.True(budgetRequest.IsCompleted);
        Assert.False(bid.RequiresAction);
        Assert.False(offer.RequiresAction);
        Assert.Contains(_state.Notifications, n => n.Subject.StartsWith("Welcome to "));
    }

    [Fact]
    public void DeclineJobOffer_KeepsMeWhereIAm()
    {
        _state.BoardConfidence = 90;
        _helper.TryMakeJobOffer();
        var offer = PendingOffer();

        _helper.DeclineJobOffer(offer);

        Assert.Equal(3, _state.MyClubId);
        Assert.Equal(90, _state.BoardConfidence);
        Assert.False(offer.RequiresAction);
    }
}
