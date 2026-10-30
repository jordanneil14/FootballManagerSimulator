using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Helpers;

public class PhysioHelperTests
{
    private readonly StateModel _state = TestData.StateWithClubs(3, new DateOnly(2026, 7, 20), myClubId: 1);
    private readonly PhysioHelper _helper;

    public PhysioHelperTests() => _helper = new PhysioHelper(_state);

    private ClubModel MyClub => _state.Clubs[0];

    [Fact]
    public void HirePhysio_ChargesTheFirstMonthAndIsNextPaidAMonthLater()
    {
        var budget = MyClub.TransferBudget;

        var message = _helper.HirePhysio();

        var physio = Assert.Single(_state.Physios);
        Assert.Equal(1, physio.Id);
        Assert.False(string.IsNullOrWhiteSpace(physio.Name));
        Assert.Equal(_state.Date, physio.HiredDate);
        Assert.Equal(_state.Date.AddMonths(1), physio.NextPaymentDate);
        Assert.Equal(budget - PhysioRules.MonthlyWage, MyClub.TransferBudget);
        Assert.Contains($"{physio.Name} has joined", message);
        Assert.Contains("reduced by 15%", message);
    }

    [Fact]
    public void HirePhysio_OnlyChargesMyClub()
    {
        var otherBudgets = _state.Clubs.Skip(1).Select(c => c.TransferBudget).ToList();

        _helper.HirePhysio();

        Assert.Equal(otherBudgets, _state.Clubs.Skip(1).Select(c => c.TransferBudget));
    }

    [Fact]
    public void HirePhysio_EachHireRaisesTheReductionUpToTheLimit()
    {
        Assert.Contains("reduced by 15%", _helper.HirePhysio());
        Assert.Contains("reduced by 25%", _helper.HirePhysio());
        Assert.Contains("reduced by 30%", _helper.HirePhysio());

        var budget = MyClub.TransferBudget;
        Assert.Equal($"You can employ at most {PhysioRules.MaxPhysios} physios.", _helper.HirePhysio());
        Assert.Equal(PhysioRules.MaxPhysios, _state.Physios.Count);
        Assert.Equal(budget, MyClub.TransferBudget);
    }

    [Fact]
    public void HirePhysio_GivesEachPhysioANewId()
    {
        _helper.HirePhysio();
        _helper.HirePhysio();
        _helper.DismissPhysio(1);
        _helper.HirePhysio();

        Assert.Equal([2, 3], _state.Physios.Select(p => p.Id));
    }

    [Fact]
    public void HirePhysio_IsAffordableWithExactlyOneMonthsWage()
    {
        MyClub.TransferBudget = PhysioRules.MonthlyWage;

        _helper.HirePhysio();

        Assert.Single(_state.Physios);
        Assert.Equal(0, MyClub.TransferBudget);
    }

    [Fact]
    public void HirePhysio_RefusedWhenTheBudgetIsTooLow()
    {
        MyClub.TransferBudget = PhysioRules.MonthlyWage - 1;

        Assert.Contains("cannot fund", _helper.HirePhysio());
        Assert.Empty(_state.Physios);
        Assert.Equal(PhysioRules.MonthlyWage - 1, MyClub.TransferBudget);
    }

    [Fact]
    public void HirePhysio_RefusedWhileUnemployed()
    {
        _state.MyClubId = null;

        Assert.Equal("You need a club to hire physios.", _helper.HirePhysio());
        Assert.Empty(_state.Physios);
    }

    [Fact]
    public void DismissPhysio_RemovesOnlyThatPhysioAndLowersTheReduction()
    {
        _helper.HirePhysio();
        _helper.HirePhysio();
        var budget = MyClub.TransferBudget;

        var message = _helper.DismissPhysio(1);

        Assert.Equal(2, Assert.Single(_state.Physios).Id);
        Assert.Contains("has been dismissed", message);
        Assert.Contains("now 15%", message);
        Assert.Equal(budget, MyClub.TransferBudget);
    }

    [Fact]
    public void DismissPhysio_UnknownIdChangesNothing()
    {
        _helper.HirePhysio();

        Assert.Equal("Physio not found.", _helper.DismissPhysio(99));
        Assert.Single(_state.Physios);
    }

    [Fact]
    public void PhysioHelper_IsRegisteredWithTheGame()
    {
        using var host = new GameHost(new SettingsModel());

        Assert.IsType<PhysioHelper>(host.Get<IPhysioHelper>());
    }
}
