using FootballManagerSimulator.Maui.Factories;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Helpers;

public class ScoutHelperTests
{
    private readonly StateModel _state = TestData.StateWithClubs(3, new DateOnly(2026, 7, 20), myClubId: 1);
    private readonly ScoutHelper _helper;

    public ScoutHelperTests()
    {
        _helper = new ScoutHelper(_state, new PlayerHelper(_state), new NotificationFactory(_state));
    }

    private ClubModel MyClub => _state.Clubs[0];

    [Fact]
    public void SeedDiscoveredPlayers_MixesFreeAgentsAndContractedButNotMySquad()
    {
        for (var i = 0; i < 200; i++)
            _state.Players.Add(TestData.Player(1000 + i, clubId: null));

        _helper.SeedDiscoveredPlayers();

        var discovered = _state.Players.Where(p => _state.DiscoveredPlayerIds.Contains(p.Id)).ToList();
        Assert.InRange(discovered.Count, ScoutRules.StartingDiscoveredMin, ScoutRules.StartingDiscoveredMax);
        Assert.Contains(discovered, p => p.Contract == null);
        Assert.Contains(discovered, p => p.Contract != null);
        Assert.DoesNotContain(discovered, p => p.Contract?.ClubId == MyClub.Id);
    }

    // My squad's 18 players are rated 60-64, averaging just under 61.9.
    [Theory]
    [InlineData(55, true)]
    [InlineData(66, true)]
    [InlineData(67, false)]
    [InlineData(90, false)]
    public void WillSign_RefusesPlayersMoreThan5AboveMySquadAverage(int rating, bool willSign)
    {
        var player = TestData.Player(1000, clubId: null, rating: rating);

        Assert.Equal(willSign, _helper.WillSign(player));
    }

    [Fact]
    public void WillSign_CountsOnlyMyBest20Players()
    {
        _state.Players.Add(TestData.Player(498, MyClub.Id, rating: 62));
        _state.Players.Add(TestData.Player(499, MyClub.Id, rating: 62));
        for (var i = 0; i < 10; i++)
            _state.Players.Add(TestData.Player(500 + i, MyClub.Id, rating: 30));

        Assert.True(_helper.WillSign(TestData.Player(1000, clubId: null, rating: 66)));
    }

    [Fact]
    public void WillSign_RetiringPlayersNeverSign()
    {
        var player = TestData.Player(1000, clubId: null, rating: 40);
        player.BirthDate = "01/01/1980";

        Assert.False(_helper.WillSign(player));
    }

    [Fact]
    public void WillSign_MyOwnPlayersAlwaysWill()
    {
        var player = TestData.Player(1000, MyClub.Id, rating: 90);

        Assert.True(_helper.WillSign(player));
    }

    [Fact]
    public void WillSign_AnyoneWillWhileIAmUnemployedOrMyClubHasNoPlayers()
    {
        var star = TestData.Player(1000, clubId: null, rating: 90);

        _state.Players.RemoveAll(p => p.Contract?.ClubId == MyClub.Id);
        Assert.True(_helper.WillSign(star));

        _state.MyClubId = null;
        Assert.True(_helper.WillSign(star));
    }

    [Fact]
    public void HireScout_ChargesTheFirstMonthAndRespectsTheLimit()
    {
        var budget = MyClub.TransferBudget;

        _helper.HireScout();

        var scout = Assert.Single(_state.Scouts);
        Assert.Equal(budget - ScoutRules.MonthlyWage, MyClub.TransferBudget);
        Assert.Equal(_state.Date.AddMonths(1), scout.NextReportDate);

        for (var i = 1; i < ScoutRules.MaxScouts; i++)
            _helper.HireScout();
        Assert.Contains("at most", _helper.HireScout());
        Assert.Equal(ScoutRules.MaxScouts, _state.Scouts.Count);
    }

    [Fact]
    public void HireScout_RefusedWhenTheBudgetIsTooLow()
    {
        MyClub.TransferBudget = ScoutRules.MonthlyWage - 1;
        Assert.Contains("cannot fund", _helper.HireScout());
        Assert.Empty(_state.Scouts);
    }

    [Fact]
    public void DismissScout_RemovesThem()
    {
        _helper.HireScout();
        _helper.DismissScout(_state.Scouts[0].Id);
        Assert.Empty(_state.Scouts);
    }

    [Fact]
    public void ProcessScouts_ReportsMonthlyWithUpToThreeMatchingPlayers()
    {
        _helper.HireScout();
        var scout = _state.Scouts[0];
        _helper.SetAssignment(scout.Id, new ScoutAssignmentModel { Position = "ST", FreeAgent = false });
        var budget = MyClub.TransferBudget;

        _state.Date = scout.NextReportDate.AddDays(-1);
        _helper.ProcessScouts();
        Assert.Empty(_state.DiscoveredPlayerIds);

        _state.Date = scout.NextReportDate;
        _helper.ProcessScouts();

        var found = _state.Players.Where(p => _state.DiscoveredPlayerIds.Contains(p.Id)).ToList();
        Assert.InRange(found.Count, 1, ScoutRules.MaxPlayersPerReport);
        Assert.All(found, p =>
        {
            Assert.Equal("ST", p.PreferredPosition);
            Assert.NotEqual(MyClub.Id, p.Contract?.ClubId);
        });
        Assert.Equal(budget - ScoutRules.MonthlyWage, MyClub.TransferBudget);
        var report = Assert.Single(_state.Notifications);
        Assert.Equal("Scouting Report", report.Subject);
        Assert.All(found, p => Assert.Contains(p.Name, report.Message));
        Assert.Equal(_state.Date.AddMonths(1), scout.NextReportDate);
    }

    [Fact]
    public void MatchesAssignment_FindsPlayersWithTheRoleAmongSeveralPositions()
    {
        var player = TestData.Player(5000, clubId: null, position: "CAM/CM");

        Assert.True(_helper.MatchesAssignment(player, new ScoutAssignmentModel { Position = "CAM" }));
        Assert.True(_helper.MatchesAssignment(player, new ScoutAssignmentModel { Position = "CM" }));
        Assert.False(_helper.MatchesAssignment(player, new ScoutAssignmentModel { Position = "ST" }));
    }

    // Aged 25 on the state date, rated 62, at club 2.
    private PlayerModel AssignmentCandidate(int? clubId = 2, int rating = 62)
    {
        var player = TestData.Player(5000, clubId, position: "CM", rating: rating);
        player.BirthDate = "01/01/2001";
        return player;
    }

    [Fact]
    public void MatchesAssignment_AnEmptyAssignmentMatchesAnyone()
    {
        Assert.True(_helper.MatchesAssignment(AssignmentCandidate(), new ScoutAssignmentModel()));
        Assert.True(_helper.MatchesAssignment(AssignmentCandidate(clubId: null), new ScoutAssignmentModel()));
    }

    [Theory]
    [InlineData(true, null, true)]
    [InlineData(true, 2, false)]
    [InlineData(false, null, false)]
    [InlineData(false, 2, true)]
    public void MatchesAssignment_FiltersFreeAgentsOrContractedPlayers(bool wantFreeAgent, int? clubId, bool matches)
    {
        var assignment = new ScoutAssignmentModel { FreeAgent = wantFreeAgent };

        Assert.Equal(matches, _helper.MatchesAssignment(AssignmentCandidate(clubId), assignment));
    }

    [Theory]
    [InlineData(25, null, true)]
    [InlineData(26, null, false)]
    [InlineData(null, 25, true)]
    [InlineData(null, 24, false)]
    public void MatchesAssignment_FiltersByAgeInclusively(int? minAge, int? maxAge, bool matches)
    {
        var assignment = new ScoutAssignmentModel { MinAge = minAge, MaxAge = maxAge };

        Assert.Equal(matches, _helper.MatchesAssignment(AssignmentCandidate(), assignment));
    }

    [Theory]
    [InlineData(62, null, true)]
    [InlineData(63, null, false)]
    [InlineData(null, 62, true)]
    [InlineData(null, 61, false)]
    public void MatchesAssignment_FiltersByRatingInclusively(int? minRating, int? maxRating, bool matches)
    {
        var assignment = new ScoutAssignmentModel { MinRating = minRating, MaxRating = maxRating };

        Assert.Equal(matches, _helper.MatchesAssignment(AssignmentCandidate(), assignment));
    }

    [Fact]
    public void MatchesAssignment_FiltersByTransferValueInclusively()
    {
        var player = AssignmentCandidate();
        var value = new PlayerHelper(_state).GetTransferValue(player);

        Assert.True(_helper.MatchesAssignment(player, new ScoutAssignmentModel { MaxValue = value }));
        Assert.False(_helper.MatchesAssignment(player, new ScoutAssignmentModel { MaxValue = value - 1 }));
    }

    [Fact]
    public void MatchesAssignment_OnlyWillSignLeavesOutPlayersWhoWouldRefuse()
    {
        var star = AssignmentCandidate(rating: 90);

        Assert.False(_helper.MatchesAssignment(star, new ScoutAssignmentModel { OnlyWillSign = true }));
        Assert.True(_helper.MatchesAssignment(star, new ScoutAssignmentModel { OnlyWillSign = false }));
        Assert.True(_helper.MatchesAssignment(AssignmentCandidate(), new ScoutAssignmentModel { OnlyWillSign = true }));
    }

    [Fact]
    public void DiscoveredPlayers_LeavesOutMySquadAndPlayersNoLongerInTheGame()
    {
        var mine = _state.Players.First(p => p.Contract?.ClubId == MyClub.Id);
        var theirs = _state.Players.First(p => p.Contract?.ClubId == 2);
        var freeAgent = TestData.Player(5000, clubId: null);
        _state.Players.Add(freeAgent);
        _state.DiscoveredPlayerIds = [mine.Id, theirs.Id, freeAgent.Id, 99_999];

        Assert.Equal([theirs, freeAgent], _helper.DiscoveredPlayers().OrderBy(p => p.Id == freeAgent.Id));
    }

    [Fact]
    public void HireScout_RefusedWhileUnemployed()
    {
        _state.MyClubId = null;

        Assert.Equal("You need a club to hire scouts.", _helper.HireScout());
        Assert.Empty(_state.Scouts);
    }

    [Fact]
    public void UnknownScouts_CannotBeDismissedOrGivenAnAssignment()
    {
        _helper.HireScout();

        Assert.Equal("Scout not found.", _helper.DismissScout(99));
        Assert.Equal("Scout not found.", _helper.SetAssignment(99, new ScoutAssignmentModel()));
        Assert.Single(_state.Scouts);
    }

    [Fact]
    public void SetAssignment_ClearingItSendsTheScoutBackToSearchingForAnyone()
    {
        _helper.HireScout();
        var scout = _state.Scouts[0];
        _helper.SetAssignment(scout.Id, new ScoutAssignmentModel { Position = "ST" });

        var message = _helper.SetAssignment(scout.Id, null);

        Assert.Null(scout.Assignment);
        Assert.Equal($"{scout.Name} will search for any player.", message);
    }

    [Fact]
    public void ProcessScouts_RecommendsChangingTheAssignmentWhenNothingIsFound()
    {
        _helper.HireScout();
        var scout = _state.Scouts[0];
        _helper.SetAssignment(scout.Id, new ScoutAssignmentModel { MinRating = 99 });

        _state.Date = scout.NextReportDate;
        _helper.ProcessScouts();

        Assert.Empty(_state.DiscoveredPlayerIds);
        Assert.Contains("change my assignment", Assert.Single(_state.Notifications).Message);
    }

    [Fact]
    public void ProcessScouts_ScoutLeavesWhenTheWageCannotBePaid()
    {
        _helper.HireScout();
        MyClub.TransferBudget = 0;

        _state.Date = _state.Scouts[0].NextReportDate;
        _helper.ProcessScouts();

        Assert.Empty(_state.Scouts);
        Assert.Contains(_state.Notifications, n => n.Subject == "Scout Left");
    }
}
