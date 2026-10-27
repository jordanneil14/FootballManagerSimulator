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
