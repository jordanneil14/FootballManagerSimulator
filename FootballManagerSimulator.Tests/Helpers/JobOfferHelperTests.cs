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
    public void VacanciesAndManagersUnderThreat_AreSeparateLists()
    {
        _state.Clubs.Single(p => p.Id == 1).JobSecurity = 14;
        _state.Clubs.Single(p => p.Id == 2).JobSecurity = 15;
        _state.Clubs.Single(p => p.Id == 3).JobSecurity = 0; // mine
        var vacant = _state.Clubs.Single(p => p.Id == 6);
        vacant.JobSecurity = 3;
        vacant.VacantUntil = _state.Date.AddDays(7);

        Assert.Equal([6], _helper.Vacancies().Select(p => p.Id));
        Assert.Equal([1], _helper.ManagersUnderThreat().Select(p => p.Id));
    }

    [Fact]
    public void ProcessVacancies_AppointsAReplacementOnceTheVacancyEnds()
    {
        var club = _state.Clubs.Single(p => p.Id == 6);
        club.JobSecurity = 4;
        club.VacantUntil = _state.Date.AddDays(1);

        _helper.ProcessVacancies();
        Assert.NotNull(club.VacantUntil);

        _state.Date = _state.Date.AddDays(1);
        _helper.ProcessVacancies();

        Assert.Null(club.VacantUntil);
        Assert.Equal(BoardConfidenceRules.Starting, club.JobSecurity);
        Assert.Contains(_state.Notifications, n => n.Subject == $"{club.Name} appoint a new manager");
    }

    [Fact]
    public void JoinClub_SwitchesClubAndBothClubsStartAfresh()
    {
        _state.BoardConfidence = 20;
        var oldClub = _state.Clubs.Single(p => p.Id == 3);
        var newClub = _state.Clubs.Single(p => p.Id == 5);
        oldClub.JobSecurity = 20;
        newClub.JobSecurity = 4;
        newClub.VacantUntil = _state.Date.AddDays(10);

        Assert.True(_helper.JoinClub(5));
        Assert.Null(newClub.VacantUntil);

        Assert.Equal(5, _state.MyClubId);
        Assert.Equal(BoardConfidenceRules.Starting, _state.BoardConfidence);
        Assert.Equal((50, 50), (oldClub.JobSecurity, newClub.JobSecurity));
        Assert.Contains(_state.Notifications, n => n.Subject == $"Welcome to {newClub.Name}");
        Assert.False(_helper.JoinClub(5)); // already there
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

    [Fact]
    public void JoinClub_EndsTheOldJobAndStartsANewOneInTheManagerHistory()
    {
        _state.ManagerHistory.StartJob(_state.Clubs.First(p => p.Id == 3), new DateOnly(2026, 7, 1));

        _helper.JoinClub(5);

        Assert.Collection(_state.ManagerHistory.Jobs,
            old => Assert.Equal((3, _state.Date), (old.ClubId, old.EndDate)),
            current => Assert.Equal((5, (DateOnly?)null), (current.ClubId, current.EndDate)));
    }

    [Fact]
    public void SackUser_LeavesMeWithoutAClubAndClearsMyClubsBusiness()
    {
        var club = _state.Clubs.Single(p => p.Id == 3);
        club.JobSecurity = 3;
        _state.BoardConfidence = 3;
        _state.ManagerHistory.StartJob(club, new DateOnly(2026, 7, 1));
        var pendingBid = new NotificationModel { Date = _state.Date, RequiresAction = true, BidPlayerId = 1 };
        _state.Notifications.Add(pendingBid);
        var upgrade = new AcademyUpgradeEvent(_state, _notifications, _state.Date.AddMonths(1), false);
        _state.Events.Add(upgrade);

        _helper.SackUser();

        Assert.Null(_state.MyClubId);
        Assert.Equal(_state.Date, Assert.Single(_state.ManagerHistory.Jobs).EndDate);
        Assert.Null(club.VacantUntil); // replaced straight away, so it can't be rejoined
        Assert.Equal(BoardConfidenceRules.Starting, club.JobSecurity);
        Assert.False(pendingBid.RequiresAction);
        Assert.True(upgrade.IsCompleted);
        Assert.Contains(_state.Notifications, n => n.Subject == $"Sacked by {club.Name}");
        Assert.DoesNotContain(_helper.Vacancies(), p => p.Id == 3);
    }

    [Fact]
    public void ResignUser_LeavesMeWithoutAClub()
    {
        var club = _state.Clubs.Single(p => p.Id == 3);
        _state.ManagerHistory.StartJob(club, new DateOnly(2026, 7, 1));
        var upgrade = new AcademyUpgradeEvent(_state, _notifications, _state.Date.AddMonths(1), false);
        _state.Events.Add(upgrade);

        _helper.ResignUser();

        Assert.Null(_state.MyClubId);
        Assert.Equal(_state.Date, Assert.Single(_state.ManagerHistory.Jobs).EndDate);
        Assert.True(upgrade.IsCompleted);
        Assert.Contains(_state.Notifications, n => n.Subject == $"Resignation from {club.Name}");
        Assert.DoesNotContain(_helper.Vacancies(), p => p.Id == 3);
    }

    [Fact]
    public void JoinClub_WorksWhileUnemployed()
    {
        _state.ManagerHistory.StartJob(_state.Clubs.Single(p => p.Id == 3), new DateOnly(2026, 7, 1));
        _helper.SackUser();
        _state.Clubs.Single(p => p.Id == 5).VacantUntil = _state.Date.AddDays(5);

        Assert.True(_helper.JoinClub(5));

        Assert.Equal(5, _state.MyClubId);
        Assert.Equal(5, _state.ManagerHistory.CurrentJob!.ClubId);
        Assert.Equal(2, _state.ManagerHistory.Jobs.Count);
    }
}
