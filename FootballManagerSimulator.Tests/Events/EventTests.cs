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

    [Fact]
    public void IncreaseTransferBudgetRequest_AddsTenMillionAndNotifies()
    {
        Create<IncreaseTransferBudgetRequestEvent>(_host.State.Date).Execute();

        Assert.Equal(15_000_000, _myClub.TransferBudget);
        Assert.Equal("Transfer Budget Request", Assert.Single(_host.State.Notifications).Subject);
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
    public void StadiumExpansionRequest_WhenAffordableChargesTwoMillionAndSchedulesTheBuild()
    {
        Create<StadiumExpansionRequestEvent>(_host.State.Date).Execute();

        Assert.Equal(3_000_000, _myClub.TransferBudget);
        var build = Assert.Single(_host.State.Events.OfType<StadiumExpansionEvent>());
        Assert.Equal(_host.State.Date.AddMonths(3), build.TriggerDate);
        Assert.Contains("accepted", Assert.Single(_host.State.Notifications).Message);
    }

    [Fact]
    public void StadiumExpansionRequest_WhenUnaffordableIsRejected()
    {
        _myClub.TransferBudget = 1_000_000;

        Create<StadiumExpansionRequestEvent>(_host.State.Date).Execute();

        Assert.Equal(1_000_000, _myClub.TransferBudget);
        Assert.Empty(_host.State.Events);
        Assert.Contains("rejected", Assert.Single(_host.State.Notifications).Message);
    }

    [Fact]
    public void StadiumExpansion_GrowsCapacityByTwentyPercent()
    {
        _myClub.StadiumSize = 12_345;

        Create<StadiumExpansionEvent>(_host.State.Date).Execute();

        Assert.Equal(12_345 + (int)(12_345 * 0.2), _myClub.StadiumSize);
    }
}
