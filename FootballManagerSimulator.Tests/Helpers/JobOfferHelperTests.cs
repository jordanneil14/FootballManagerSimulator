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
    // Squad strengths give standings: club 5 = 0, 4 = 20, 3 = 40, 6 = 60, 2 = 80, 1 = 100.
    private static readonly Dictionary<int, int> Strengths = new() { [1] = 80, [2] = 70, [3] = 50, [4] = 40, [5] = 30, [6] = 60 };
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
        foreach (var (clubId, rating) in Strengths)
            _state.Players.Add(TestData.Player(clubId * 100, clubId, rating: rating));
        _helper = new JobOfferHelper(_state, _notifications, new ReputationHelper(_state, _notifications));
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

    // Secure, with a reputation whose offer window (-10 to +5) only takes in club 2.
    private void SecureWithOffersFromClub2()
    {
        _state.BoardConfidence = 90;
        _state.ManagerReputation = 72;
    }

    private ClubModel Club(int id) => _state.Clubs.Single(p => p.Id == id);

    private NotificationModel PendingOffer() => _state.Notifications.Single(p => p.RequiresAction && p.JobOfferClubId != null);

    [Fact]
    public void TryMakeJobOffer_NothingUnlessSecure()
    {
        _state.BoardConfidence = BoardConfidenceRules.SecureFrom - 1;

        Assert.False(_helper.TryMakeJobOffer());
        Assert.Empty(_state.Notifications);
    }

    [Fact]
    public void TryMakeJobOffer_OnlyFromClubsAtMyLevelOrAStepAbove()
    {
        SecureWithOffersFromClub2();
        var offeredBy = new HashSet<int>();

        for (var i = 0; i < 30; i++)
        {
            Assert.True(_helper.TryMakeJobOffer());
            var offer = PendingOffer();
            offeredBy.Add(offer.JobOfferClubId!.Value);
            _helper.DeclineJobOffer(offer);
        }

        // 75 after the Secure bonus: club 1 (gap -25) is too far above and club 6 (+15) too far below.
        Assert.Equal([2], offeredBy);
    }

    [Fact]
    public void TryMakeJobOffer_ComesFromAnyCountry()
    {
        _state.BoardConfidence = 90;
        _state.ManagerReputation = 55; // 58 after the Secure bonus: club 6 (60) is the only one in the window

        Assert.True(_helper.TryMakeJobOffer());

        Assert.Equal(6, PendingOffer().JobOfferClubId);
    }

    [Fact]
    public void TryMakeJobOffer_NothingWhenNoClubIsAtMyLevel()
    {
        _state.BoardConfidence = 90;
        _state.ManagerReputation = 30; // 33: club 4 is 13 below and club 6 is 27 above

        Assert.False(_helper.TryMakeJobOffer());
        Assert.Empty(_state.Notifications);
    }

    [Fact]
    public void TryMakeJobOffer_FavoursClubsWhoseManagerIsStruggling()
    {
        _state.BoardConfidence = 90;
        _state.ManagerReputation = 87; // 90 after the Secure bonus
        // Clubs 1 and 2 share the top squad strength, so both stand at 90: a gap of 0.
        foreach (var player in _state.Players.Where(p => p.Contract!.ClubId == 2)) player.Rating = 80;
        Club(1).JobSecurity = 0;
        Club(2).JobSecurity = 100;
        var fromClub1 = 0;

        for (var i = 0; i < 200; i++)
        {
            _helper.TryMakeJobOffer();
            var offer = PendingOffer();
            if (offer.JobOfferClubId == 1) fromClub1++;
            _helper.DeclineJobOffer(offer);
        }

        // Weighted 101 to 1.
        Assert.InRange(fromClub1, 185, 200);
    }

    [Fact]
    public void TryMakeJobOffer_OnlyOneOfferAtATime()
    {
        SecureWithOffersFromClub2();

        Assert.True(_helper.TryMakeJobOffer());
        Assert.False(_helper.TryMakeJobOffer());
        Assert.Single(_state.Notifications);
    }

    [Fact]
    public void TryMakeJobOffer_NamesTheClubAndItsLeague()
    {
        SecureWithOffersFromClub2();

        _helper.TryMakeJobOffer();

        var offer = PendingOffer();
        var club = _state.Clubs.Single(c => c.Id == offer.JobOfferClubId);
        Assert.Equal($"Job offer from {club.Name}", offer.Subject);
        Assert.Equal($"{club.Name} Chairman", offer.Recipient);
        Assert.Contains($"({_state.Competitions.Single(c => c.Id == club.LeagueId).Name})", offer.Message);
    }

    [Fact]
    public void ProcessJobOffers_OnlySometimesMakesAnOffer()
    {
        SecureWithOffersFromClub2();
        var offers = 0;

        for (var week = 0; week < 400; week++)
        {
            _helper.ProcessJobOffers();
            if (_state.Notifications.Any(p => p.RequiresAction))
            {
                offers++;
                _helper.DeclineJobOffer(PendingOffer());
            }
        }

        // 15% a week: about 60 of 400.
        Assert.InRange(offers, 20, 120);
    }

    [Fact]
    public void AcceptJobOffer_FromAClubThatNoLongerExistsLeavesMeWhereIAm()
    {
        var offer = new NotificationModel { Date = _state.Date, Subject = "Job offer", RequiresAction = true, JobOfferClubId = 99 };
        _state.Notifications.Add(offer);

        _helper.AcceptJobOffer(offer);

        Assert.Equal(3, _state.MyClubId);
        Assert.False(offer.RequiresAction);
        Assert.Equal("⚠️ Job no longer available", offer.Response);
    }

    [Fact]
    public void AnsweringAJobOfferTwiceChangesNothing()
    {
        SecureWithOffersFromClub2();
        _helper.TryMakeJobOffer();
        var offer = PendingOffer();
        _helper.DeclineJobOffer(offer);

        _helper.AcceptJobOffer(offer);
        _helper.DeclineJobOffer(offer);

        Assert.Equal(3, _state.MyClubId);
        Assert.Equal("❌ Job declined", offer.Response);
    }

    [Fact]
    public void AnsweringANotificationThatIsNotAJobOfferChangesNothing()
    {
        var bid = new NotificationModel { Date = _state.Date, Subject = "Bid", RequiresAction = true, BidPlayerId = 1 };
        _state.Notifications.Add(bid);

        _helper.AcceptJobOffer(bid);
        _helper.DeclineJobOffer(bid);

        Assert.True(bid.RequiresAction);
        Assert.Null(bid.Response);
        Assert.Equal(3, _state.MyClubId);
    }

    [Fact]
    public void AcceptJobOffer_MovesMeToTheNewClubAndCancelsOldClubBusiness()
    {
        SecureWithOffersFromClub2();
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
        _state.ShowManagerNotifications = true;
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
        SecureWithOffersFromClub2();
        _helper.TryMakeJobOffer();
        var offer = PendingOffer();

        _helper.DeclineJobOffer(offer);

        Assert.Equal(3, _state.MyClubId);
        Assert.Equal(90, _state.BoardConfidence);
        Assert.False(offer.RequiresAction);
        Assert.Equal("❌ Job declined", offer.Response);
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
    public void LeavingAClub_RefundsItsUnfinishedBuilds()
    {
        var club = _state.Clubs.Single(p => p.Id == 3);
        club.TransferBudget = 1_000_000;
        club.StadiumSize = 10_000;
        club.AcademyRating = 4;
        var stadium = new StadiumExpansionEvent(_state, _notifications, _state.Date.AddMonths(6), false);
        var academy = new AcademyUpgradeEvent(_state, _notifications, _state.Date.AddMonths(1), false);
        var finished = new StadiumExpansionEvent(_state, _notifications, _state.Date.AddMonths(-1), true);
        _state.Events.AddRange([stadium, academy, finished]);

        _helper.JoinClub(5);

        Assert.Equal(1_000_000 + StadiumRules.UpgradeCost(10_000) + AcademyRules.UpgradeCost(5), club.TransferBudget);
        Assert.True(stadium.IsCompleted);
        Assert.Equal(10_000, club.StadiumSize);
    }

    [Fact]
    public void BeingSacked_RefundsAnUnfinishedStadiumBuild()
    {
        var club = _state.Clubs.Single(p => p.Id == 3);
        club.TransferBudget = 0;
        club.StadiumSize = 17_300;
        _state.Events.Add(new StadiumExpansionEvent(_state, _notifications, _state.Date.AddMonths(9), false));

        _helper.SackUser();

        Assert.Equal(1_620_000, club.TransferBudget);
    }

    [Fact]
    public void LeavingAClub_RefundsNothingForABudgetRequest()
    {
        var club = _state.Clubs.Single(p => p.Id == 3);
        club.TransferBudget = 1_000_000;
        var request = new IncreaseTransferBudgetRequestEvent(_state, _notifications, _state.Date.AddDays(2), false);
        _state.Events.Add(request);

        _helper.ResignUser();

        Assert.True(request.IsCompleted);
        Assert.Equal(1_000_000, club.TransferBudget);
    }

    [Fact]
    public void LeavingAClub_RefundsOnlyTheClubThatPaid()
    {
        var oldClub = _state.Clubs.Single(p => p.Id == 3);
        var newClub = _state.Clubs.Single(p => p.Id == 5);
        oldClub.StadiumSize = 10_000;
        oldClub.TransferBudget = 1_000_000;
        newClub.TransferBudget = 2_000_000;
        _state.Events.Add(new StadiumExpansionEvent(_state, _notifications, _state.Date.AddMonths(6), false));

        _helper.JoinClub(5);

        Assert.Equal(2_000_000, newClub.TransferBudget);
        Assert.Equal(1_000_000 + StadiumRules.UpgradeCost(10_000), oldClub.TransferBudget);
    }

    [Fact]
    public void LeavingAClub_LeavesItsPhysiosBehind()
    {
        _state.Physios.Add(new PhysioModel { Id = 1, Name = "Sam Physio", HiredDate = _state.Date });

        _helper.ResignUser();

        Assert.Empty(_state.Physios);
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
    public void TryMakeJobOffer_NoOffersWhileUnemployed()
    {
        _state.ManagerHistory.StartJob(_state.Clubs.Single(p => p.Id == 3), new DateOnly(2026, 7, 1));
        _helper.SackUser();
        _state.BoardConfidence = 100;

        Assert.False(_helper.TryMakeJobOffer());
        Assert.DoesNotContain(_state.Notifications, n => n.JobOfferClubId != null);
    }

    [Fact]
    public void SackUser_DoesNothingWhenAlreadyUnemployed()
    {
        _state.MyClubId = null;

        _helper.SackUser();
        _helper.ResignUser();

        Assert.Empty(_state.Notifications);
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

    // ---------------------------------------------------------------------------------
    // Reputation and chances
    // ---------------------------------------------------------------------------------

    [Fact]
    public void Reputation_StartsTenBelowMyClub()
    {
        var reputation = new ReputationHelper(_state, _notifications);

        Assert.Equal(30, reputation.Reputation); // club 3 stands at 40
        Assert.Equal(30, _state.ManagerReputation);
    }

    [Fact]
    public void Reputation_NeverStartsBelowTheFloor()
    {
        _state.MyClubId = 5; // stands at 0

        Assert.Equal(JobRules.Floor, new ReputationHelper(_state, _notifications).Reputation);
    }

    [Fact]
    public void Reputation_WhenUnemployedStartsFromTheLastClub()
    {
        _state.MyClubId = null;
        _state.ManagerHistory.StartJob(Club(6), new DateOnly(2026, 7, 1));
        _state.ManagerHistory.EndJob(_state.Date);

        Assert.Equal(50, new ReputationHelper(_state, _notifications).Reputation);
    }

    [Fact]
    public void Reputation_WithNoHistoryStartsAt20()
    {
        _state.MyClubId = null;

        Assert.Equal(JobRules.NoHistoryStart, new ReputationHelper(_state, _notifications).Reputation);
    }

    [Fact]
    public void Standings_ArePercentilesOfSquadStrength()
    {
        var standings = new ReputationHelper(_state, _notifications).Standings();

        Assert.Equal([100.0, 80.0, 40.0, 20.0, 0.0, 60.0], Enumerable.Range(1, 6).Select(id => standings[id]));
    }

    [Fact]
    public void Standings_JudgeEachClubOnItsBestTwentyPlayers()
    {
        // 20 players rated 90 and a long tail rated 1 still makes club 5 the strongest.
        for (var i = 0; i < 20; i++) _state.Players.Add(TestData.Player(1000 + i, 5, rating: 90));
        for (var i = 0; i < 30; i++) _state.Players.Add(TestData.Player(2000 + i, 5, rating: 1));

        Assert.Equal(100, new ReputationHelper(_state, _notifications).Standings()[5]);
    }

    [Theory]
    [InlineData(-5, JobChance.Interested)]
    [InlineData(-5.01, JobChance.Possible)]
    [InlineData(-15, JobChance.Possible)]
    [InlineData(-15.01, JobChance.LongShot)]
    [InlineData(-25, JobChance.LongShot)]
    [InlineData(-25.01, JobChance.OutOfReach)]
    [InlineData(40, JobChance.Interested)]
    public void ChanceFor_Bands(double gap, JobChance expected) => Assert.Equal(expected, JobRules.ChanceFor(gap));

    [Theory]
    [InlineData(0, 0, 0.9)]
    [InlineData(30, 0, 0.9)]
    [InlineData(30.01, 0, 0.6)] // well below you
    [InlineData(-10, 0, 0.5)]
    [InlineData(-20, 0, 0.15)]
    [InlineData(-30, 0, 0)]
    [InlineData(-10, 89, 0.5)]
    [InlineData(-10, 90, 0.9)] // out of work long enough that clubs stop being fussy
    [InlineData(31, 90, 0.9)]
    [InlineData(-20, 90, 0.15)] // ...but not about long shots
    public void AcceptChance_ByGapAndTimeOutOfWork(double gap, int daysUnemployed, double expected) =>
        Assert.Equal(expected, JobRules.AcceptChance(gap, daysUnemployed));

    [Theory]
    [InlineData(50, 30)] // Stable: no adjustment
    [InlineData(29, 25)] // Under pressure: -5
    [InlineData(10, 25)] // At risk: -5
    [InlineData(75, 33)] // Secure: +3
    public void Gap_DependsOnHowThingsAreGoingAtMyClub(double confidence, double expected)
    {
        _state.BoardConfidence = confidence;
        _state.ManagerReputation = 30;

        Assert.Equal(expected, _helper.Gap(Club(5))); // standing 0
    }

    [Fact]
    public void Gap_UnemployedHasNoAdjustment()
    {
        _state.MyClubId = null;
        _state.BoardConfidence = 0;
        _state.ManagerReputation = 30;

        Assert.Equal(30, _helper.Gap(Club(5)));
    }

    [Fact]
    public void TheClubThatSackedMe_IsOutOfReachUntilTheEndOfNextSeason()
    {
        _state.ManagerReputation = 90;
        _state.ManagerHistory.StartJob(Club(3), new DateOnly(2026, 7, 1));
        _helper.SackUser(); // 7 Sep 2026

        Assert.Equal(JobChance.OutOfReach, _helper.ChanceFor(Club(3)));
        _state.Date = new DateOnly(2028, 6, 30);
        Assert.Equal(JobChance.OutOfReach, _helper.ChanceFor(Club(3)));
        _state.Date = new DateOnly(2028, 7, 1);
        Assert.Equal(JobChance.Interested, _helper.ChanceFor(Club(3)));
    }

    [Fact]
    public void BeingSacked_CostsReputationAndResigningCostsLess()
    {
        _state.ManagerReputation = 50;
        _helper.SackUser();
        Assert.Equal(50 + JobRules.Sacked, _state.ManagerReputation);
        Assert.Contains("Your reputation has fallen to 42", _state.Notifications.Last().Message);

        _state.MyClubId = 3;
        _state.ManagerReputation = 50;
        _helper.ResignUser();
        Assert.Equal(50 + JobRules.Resigned, _state.ManagerReputation);
    }

    [Fact]
    public void MovingToANewJob_IsNotAResignation()
    {
        _state.ManagerReputation = 50;

        _helper.JoinClub(5);

        Assert.Equal(50, _state.ManagerReputation);
    }

    [Fact]
    public void JoinClub_RecordsWhereTheSquadIsExpectedToFinish()
    {
        // Club 4 joins club 5 in league 30, where club 5 now has the stronger squad.
        _state.Players.Add(TestData.Player(9000, 5, rating: 99));
        Club(4).LeagueId = 30;

        _helper.JoinClub(4);

        Assert.Equal(2, _state.ManagerHistory.CurrentJob!.ExpectedFinish);
    }

    // ---------------------------------------------------------------------------------
    // Applications
    // ---------------------------------------------------------------------------------

    private ClubModel Vacant(int id, int days = 14)
    {
        var club = Club(id);
        club.VacantUntil = _state.Date.AddDays(days);
        return club;
    }

    [Fact]
    public void Apply_AddsAnApplicationDecidedInTwoToFourDays()
    {
        Vacant(5);

        Assert.True(_helper.Apply(5));

        var application = Assert.Single(_state.JobApplications);
        Assert.Equal((5, _state.Date), (application.ClubId, application.AppliedOn));
        Assert.InRange(application.DecideOn.DayNumber - _state.Date.DayNumber, JobRules.DecisionMinDays, JobRules.DecisionMaxDays);
    }

    [Theory]
    [InlineData(1, 1)] // closes tomorrow: decided tomorrow, before the club appoints anyone
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    public void Apply_IsDecidedBeforeTheVacancyCloses(int vacancyDays, int latestDecisionDays)
    {
        Vacant(5, vacancyDays);

        _helper.Apply(5);

        var decideOn = _state.JobApplications.Single().DecideOn;
        Assert.InRange(decideOn.DayNumber - _state.Date.DayNumber, 1, latestDecisionDays);
    }

    [Fact]
    public void Apply_RefusedWhenOutOfReach()
    {
        Vacant(1); // standing 100, reputation 30

        Assert.Equal(JobChance.OutOfReach, _helper.ChanceFor(Club(1)));
        Assert.NotNull(_helper.ApplyRefusal(Club(1)));
        Assert.False(_helper.Apply(1));
        Assert.Empty(_state.JobApplications);
    }

    [Fact]
    public void Apply_RefusedWhenTheClubIsNotLookingOrIsMine()
    {
        Assert.False(_helper.Apply(5));
        Vacant(3);
        Assert.False(_helper.Apply(3));
    }

    [Fact]
    public void Apply_OnceToEachClubAndThreeAtATime()
    {
        _state.ManagerReputation = 100;
        foreach (var id in new[] { 1, 2, 4, 5 }) Vacant(id);

        Assert.True(_helper.Apply(1));
        Assert.False(_helper.Apply(1));
        Assert.True(_helper.Apply(2));
        Assert.True(_helper.Apply(4));
        Assert.False(_helper.Apply(5));
        Assert.Equal(3, _state.JobApplications.Count);
    }

    [Fact]
    public void ProcessApplications_WaitsForTheDecisionDay()
    {
        Vacant(5);
        _helper.Apply(5);

        _helper.ProcessApplications();

        Assert.Single(_state.JobApplications);
        Assert.Empty(_state.Notifications);
    }

    [Fact]
    public void ProcessApplications_AnOfferCanBeAcceptedToMoveClub()
    {
        _state.ManagerReputation = 25; // Interested in club 5 (standing 0): 90%
        NotificationModel? offer = null;
        for (var i = 0; i < 50 && offer == null; i++)
        {
            _state.JobRejections.Clear();
            Vacant(5);
            _helper.Apply(5);
            _state.Date = _state.JobApplications.Single().DecideOn;
            _helper.ProcessApplications();
            offer = _state.Notifications.FirstOrDefault(n => n.RequiresAction && n.JobOfferClubId == 5);
        }

        Assert.NotNull(offer);
        Assert.Empty(_state.JobApplications);
        Assert.Equal($"{Club(5).Name} want you as manager", offer!.Subject);

        _helper.AcceptJobOffer(offer);
        Assert.Equal(5, _state.MyClubId);
    }

    [Fact]
    public void ProcessApplications_RejectionKeepsMeAwayFor90Days()
    {
        _state.ManagerReputation = 50; // club 6 (standing 60) is Possible
        Vacant(6, days: 60);
        Assert.True(_helper.Apply(6));
        _state.ManagerReputation = 0; // now out of reach, so the club is certain to say no
        _state.Date = _state.JobApplications.Single().DecideOn;
        var rejectedOn = _state.Date;

        _helper.ProcessApplications();

        Assert.Contains(_state.Notifications, n => n.Subject == $"Application to {Club(6).Name}" && !n.RequiresAction);
        Assert.Equal(rejectedOn.AddDays(90), _helper.RejectionFor(6)!.Until);

        _state.ManagerReputation = 60;
        _state.Date = rejectedOn.AddDays(89);
        Vacant(6);
        Assert.StartsWith("Rejected", _helper.ApplyRefusal(Club(6)));
        Assert.False(_helper.Apply(6));

        _state.Date = rejectedOn.AddDays(90);
        Vacant(6);
        Assert.Null(_helper.RejectionFor(6));
        Assert.True(_helper.Apply(6));
    }

    [Fact]
    public void ProcessVacancies_AFilledVacancyEndsMyApplication()
    {
        Vacant(5, days: 1);
        _helper.Apply(5);
        _state.JobApplications.Single().DecideOn = _state.Date.AddDays(5); // as if the club had been slow

        _state.Date = _state.Date.AddDays(1);
        _helper.ProcessVacancies();

        Assert.Empty(_state.JobApplications);
        Assert.Contains(_state.Notifications, n => n.Message.Contains("The position has now been filled"));
    }

    [Fact]
    public void LeavingAClub_KeepsJobOffersAndApplications()
    {
        var offer = new NotificationModel { Date = _state.Date, RequiresAction = true, JobOfferClubId = 4 };
        _state.Notifications.Add(offer);
        Vacant(5);
        _helper.Apply(5);

        _helper.JoinClub(1);

        Assert.True(offer.RequiresAction);
        Assert.Single(_state.JobApplications);
    }

    [Fact]
    public void Vacancies_BestChanceFirst()
    {
        _state.ManagerReputation = 50;
        foreach (var id in new[] { 1, 4, 5, 6 }) Vacant(id);

        // Clubs 4 and 5 are Interested (biggest first), club 6 (gap -10) Possible and club 1 (-50) out of reach.
        Assert.Equal([4, 5, 6, 1], _helper.Vacancies().Select(p => p.Id));
    }

    // ---------------------------------------------------------------------------------
    // Out of work
    // ---------------------------------------------------------------------------------

    private void UnemployedSince(DateOnly date)
    {
        _state.ManagerHistory.StartJob(Club(3), date.AddYears(-1));
        _state.ManagerHistory.EndJob(date);
        _state.MyClubId = null;
    }

    [Fact]
    public void ProcessUnemployment_ReputationSlipsEvery30DaysAfterTheFirst90()
    {
        UnemployedSince(_state.Date);
        _state.ManagerReputation = 30;
        // Keep a reachable vacancy open so no club is pushed into sacking its manager.
        Vacant(5, days: 1000);
        var start = _state.Date;

        for (var day = 1; day <= 180; day++)
        {
            _state.Date = start.AddDays(day);
            _helper.ProcessUnemployment();
        }

        Assert.Equal(27, _state.ManagerReputation); // days 120, 150 and 180
    }

    [Fact]
    public void ProcessUnemployment_ReputationNeverSlipsBelowTheFloor()
    {
        UnemployedSince(_state.Date.AddDays(-120));
        _state.ManagerReputation = 5.5;
        Vacant(5, days: 1000);

        _helper.ProcessUnemployment();

        Assert.Equal(JobRules.Floor, _state.ManagerReputation);
    }

    [Fact]
    public void ProcessUnemployment_OpensAReachableJobAfter28Days()
    {
        UnemployedSince(_state.Date.AddDays(-27));
        _state.ManagerReputation = 30;
        Club(4).JobSecurity = 20; // Under pressure, standing 20
        Club(5).JobSecurity = 60; // Stable, standing 0

        _helper.ProcessUnemployment();
        Assert.Equal(0, _helper.ReachableVacancies());

        _state.Date = _state.Date.AddDays(1);
        _helper.ProcessUnemployment();

        Assert.NotNull(Club(4).VacantUntil);
        Assert.Null(Club(5).VacantUntil);
        Assert.Equal(1, _helper.ReachableVacancies());
        Assert.Contains(_state.Notifications, n => n.Subject == $"{Club(4).Name} sack their manager");

        _state.Date = _state.Date.AddDays(1);
        _helper.ProcessUnemployment();
        Assert.Single(_state.Clubs, p => p.VacantUntil != null);
    }

    [Fact]
    public void ProcessUnemployment_FallsBackToAnyReachableClub()
    {
        UnemployedSince(_state.Date.AddDays(-28));
        _state.ManagerReputation = 30;
        Club(3).JobSecurity = 80;
        Club(4).JobSecurity = 80;
        Club(5).JobSecurity = 40;

        _helper.ProcessUnemployment();

        Assert.NotNull(Club(5).VacantUntil);
    }
}
