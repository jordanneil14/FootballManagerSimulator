using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Helpers;

public class AnalystHelperTests
{
    private readonly StateModel _state = TestData.StateWithClubs(3, new DateOnly(2026, 7, 20), myClubId: 1);
    private readonly AnalystHelper _helper;

    public AnalystHelperTests() => _helper = new AnalystHelper(_state);

    private ClubModel MyClub => _state.Clubs[0];

    [Fact]
    public void HireAnalyst_ChargesTheFirstMonthAtTheScoutsWage()
    {
        var budget = MyClub.TransferBudget;
        var otherBudgets = _state.Clubs.Skip(1).Select(c => c.TransferBudget).ToList();

        var message = _helper.HireAnalyst();

        var analyst = Assert.Single(_state.Analysts);
        Assert.Equal(ScoutRules.MonthlyWage, AnalystRules.MonthlyWage);
        Assert.False(string.IsNullOrWhiteSpace(analyst.Name));
        Assert.Equal(_state.Date, analyst.HiredDate);
        Assert.Equal(_state.Date.AddMonths(1), analyst.NextPaymentDate);
        Assert.Equal(budget - AnalystRules.MonthlyWage, MyClub.TransferBudget);
        Assert.Equal(otherBudgets, _state.Clubs.Skip(1).Select(c => c.TransferBudget));
        Assert.Contains($"{analyst.Name} has joined", message);
    }

    [Fact]
    public void HireAnalyst_OnlyOneAllowed()
    {
        _helper.HireAnalyst();
        var budget = MyClub.TransferBudget;

        Assert.Equal("You can only employ one analyst.", _helper.HireAnalyst());
        Assert.Single(_state.Analysts);
        Assert.Equal(budget, MyClub.TransferBudget);
    }

    [Fact]
    public void HireAnalyst_NeedsTheBudget()
    {
        MyClub.TransferBudget = AnalystRules.MonthlyWage - 1;

        Assert.StartsWith("The board cannot fund this.", _helper.HireAnalyst());
        Assert.Empty(_state.Analysts);
    }

    [Fact]
    public void HireAnalyst_NeedsAClub()
    {
        _state.MyClubId = null;

        Assert.Equal("You need a club to hire an analyst.", _helper.HireAnalyst());
        Assert.Empty(_state.Analysts);
    }

    [Fact]
    public void DismissAnalyst_RemovesThemAndAllowsANewHire()
    {
        _helper.HireAnalyst();
        var analyst = _state.Analysts[0];

        var message = _helper.DismissAnalyst(analyst.Id);

        Assert.Empty(_state.Analysts);
        Assert.Contains($"{analyst.Name} has been dismissed", message);
        _helper.HireAnalyst();
        Assert.Single(_state.Analysts);
    }

    [Fact]
    public void DismissAnalyst_UnknownId()
    {
        Assert.Equal("Analyst not found.", _helper.DismissAnalyst(99));
    }

    [Fact]
    public void AnalystHelper_IsRegisteredWithTheGame()
    {
        using var host = new GameHost(new SettingsModel());

        Assert.IsType<AnalystHelper>(host.Get<IAnalystHelper>());
    }
}
