using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FootballManagerSimulator.Tests.Events;

public class EventManagerTests
{
    private readonly StateModel _state = new() { Date = new DateOnly(2026, 8, 10) };
    private readonly EventManager _manager;

    public EventManagerTests() => _manager = new EventManager(_state);

    private static Mock<IEvent> EventOn(DateOnly date, bool valid = true, bool completed = false)
    {
        var mock = new Mock<IEvent>();
        mock.SetupGet(e => e.TriggerDate).Returns(date);
        mock.SetupProperty(e => e.IsCompleted, completed);
        mock.Setup(e => e.ValidateAdd()).Returns((valid, valid ? "" : "not allowed"));
        return mock;
    }

    [Fact]
    public void ValidateAndAddEvent_AddsValidEventsAndReportsInvalidOnes()
    {
        var good = EventOn(_state.Date);
        var bad = EventOn(_state.Date, valid: false);

        _manager.ValidateAndAddEvent(good.Object);
        _manager.ValidateAndAddEvent(bad.Object);

        Assert.Same(good.Object, Assert.Single(_state.Events));
        Assert.Equal("not allowed", Assert.Single(_state.UserFeedbackUpdates));
    }

    [Fact]
    public void ExecuteEvents_RunsOnlyDueIncompleteEventsOnceAndMarksThemCompleted()
    {
        var past = EventOn(_state.Date.AddDays(-3));
        var today = EventOn(_state.Date);
        var future = EventOn(_state.Date.AddDays(1));
        var done = EventOn(_state.Date, completed: true);
        _state.Events.AddRange([past.Object, today.Object, future.Object, done.Object]);

        _manager.ExecuteEvents();
        _manager.ExecuteEvents();

        past.Verify(e => e.Execute(), Times.Once);
        today.Verify(e => e.Execute(), Times.Once);
        future.Verify(e => e.Execute(), Times.Never);
        done.Verify(e => e.Execute(), Times.Never);
        Assert.True(past.Object.IsCompleted);
        Assert.False(future.Object.IsCompleted);
    }
}

public class ClubRequestEventTests : IDisposable
{
    private readonly GameHost _host = new(new SettingsModel());
    private readonly ClubModel _myClub = TestData.Club(1, "Mine", budget: 5_000_000);

    public ClubRequestEventTests()
    {
        _host.State.Date = new DateOnly(2026, 9, 1);
        _host.State.MyClubId = 1;
        _host.State.Clubs.Add(_myClub);
    }

    public void Dispose() => _host.Dispose();

    private T Create<T>(DateOnly triggerDate) where T : IEvent =>
        ActivatorUtilities.CreateInstance<T>(_host.Services, triggerDate, false);

    [Theory]
    [InlineData(1, 10_000_000)]
    [InlineData(2, 2_500_000)]
    [InlineData(3, 1_000_000)]
    [InlineData(4, 150_000)]
    [InlineData(5, 50_000)]
    public void IncreaseTransferBudgetRequest_GrantsAnAmountForTheLeagueRankAndNotifies(int rank, int amount)
    {
        var league = TestData.League(1, [_myClub]);
        league.Rank = rank;
        _host.State.Competitions.Add(league);

        Create<IncreaseTransferBudgetRequestEvent>(_host.State.Date).Execute();

        Assert.Equal(5_000_000 + amount, _myClub.TransferBudget);
        var notification = Assert.Single(_host.State.Notifications);
        Assert.Equal("Transfer Budget Request", notification.Subject);
        Assert.Contains($"£{amount:n0}", notification.Message);
    }

    [Fact]
    public void IncreaseTransferBudgetRequest_CannotBeRepeatedWithinThreeMonths()
    {
        Assert.True(Create<IncreaseTransferBudgetRequestEvent>(_host.State.Date.AddDays(2)).ValidateAdd().success);
        _host.State.Events.Add(Create<IncreaseTransferBudgetRequestEvent>(_host.State.Date.AddMonths(-2)));

        var (success, message) = Create<IncreaseTransferBudgetRequestEvent>(_host.State.Date.AddDays(2)).ValidateAdd();

        Assert.False(success);
        Assert.Contains("3 months", message);
    }

    [Fact]
    public void IncreaseTransferBudgetRequest_IsAllowedAgainAfterThreeMonths()
    {
        _host.State.Events.Add(Create<IncreaseTransferBudgetRequestEvent>(_host.State.Date.AddMonths(-4)));

        Assert.True(Create<IncreaseTransferBudgetRequestEvent>(_host.State.Date.AddDays(2)).ValidateAdd().success);
    }

    [Fact]
    public void ExpandStadium_ChargesTheBudgetAndSchedulesTheWork()
    {
        _myClub.StadiumSize = 20_000;

        Assert.Contains("approved", _host.Engine.ExpandStadium());

        Assert.Equal(5_000_000 - StadiumRules.UpgradeCost(20_000), _myClub.TransferBudget);
        var build = Assert.Single(_host.State.Events.OfType<StadiumExpansionEvent>());
        Assert.Equal(_host.State.Date.AddMonths(StadiumRules.UpgradeMonths), build.TriggerDate);
        Assert.Equal(20_000, _myClub.StadiumSize); // not until the work is done
        Assert.Contains("already underway", _host.Engine.ExpandStadium());
    }

    [Fact]
    public void ExpandStadium_IsRefusedWhenUnaffordableOrAtTheMaximum()
    {
        _myClub.StadiumSize = 80_000;
        Assert.Contains("cannot fund", _host.Engine.ExpandStadium());

        _myClub.StadiumSize = StadiumRules.MaxCapacity;
        Assert.Contains("maximum", _host.Engine.ExpandStadium());

        Assert.Equal(5_000_000, _myClub.TransferBudget);
        Assert.Empty(_host.State.Events);
    }

    [Fact]
    public void UpgradeAcademy_ChargesTheBudgetAndSchedulesTheWork()
    {
        _myClub.AcademyRating = 4;

        Assert.Contains("approved", _host.Engine.UpgradeAcademy());

        Assert.Equal(5_000_000 - AcademyRules.UpgradeCost(5), _myClub.TransferBudget);
        var upgrade = Assert.Single(_host.State.Events.OfType<AcademyUpgradeEvent>());
        Assert.Equal(_host.State.Date.AddMonths(AcademyRules.UpgradeMonths), upgrade.TriggerDate);
        Assert.Equal(4, _myClub.AcademyRating); // not until the work is done
        Assert.Contains("already underway", _host.Engine.UpgradeAcademy());
    }

    [Fact]
    public void UpgradeAcademy_IsRefusedWhenUnaffordableOrAtTheMaximum()
    {
        _myClub.AcademyRating = 9;
        Assert.Contains("cannot fund", _host.Engine.UpgradeAcademy());

        _myClub.AcademyRating = AcademyRules.Max;
        Assert.Contains("maximum", _host.Engine.UpgradeAcademy());

        Assert.Equal(5_000_000, _myClub.TransferBudget);
        Assert.Empty(_host.State.Events);
    }

    [Fact]
    public void AcademyUpgrade_RaisesTheRatingByOneAndNotifies()
    {
        _myClub.AcademyRating = 6;

        Create<AcademyUpgradeEvent>(_host.State.Date).Execute();

        Assert.Equal(7, _myClub.AcademyRating);
        Assert.Equal("Academy Upgrade", Assert.Single(_host.State.Notifications).Subject);
    }

    [Fact]
    public void StadiumExpansion_GrowsCapacityByTwentyPercent()
    {
        _myClub.StadiumSize = 12_345;

        Create<StadiumExpansionEvent>(_host.State.Date).Execute();

        Assert.Equal(12_345 + (int)(12_345 * 0.2), _myClub.StadiumSize);
        Assert.Equal("Stadium Expansion", Assert.Single(_host.State.Notifications).Subject);
    }

    [Fact]
    public void StadiumExpansion_StopsAtTheMaximumCapacity()
    {
        _myClub.StadiumSize = StadiumRules.MaxCapacity - 1_000;

        Create<StadiumExpansionEvent>(_host.State.Date).Execute();

        Assert.Equal(StadiumRules.MaxCapacity, _myClub.StadiumSize);
    }
}
