using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class PhysioRulesTests
{
    [Theory]
    [InlineData(0, 0.0, "0%")]
    [InlineData(1, 0.15, "15%")]
    [InlineData(2, 0.25, "25%")]
    [InlineData(3, 0.30, "30%")]
    [InlineData(4, 0.30, "30%")]
    public void ReductionAndLabelGrowWithEachPhysioUpToTheCap(int physios, double reduction, string label)
    {
        Assert.Equal(reduction, PhysioRules.InjuryReduction(physios), precision: 10);
        Assert.Equal(label, PhysioRules.InjuryReductionLabel(physios));
    }

    [Fact]
    public void LabelMatchesTheReductionForEveryHireableCount()
    {
        for (var physios = 0; physios <= PhysioRules.MaxPhysios; physios++)
            Assert.Equal($"{PhysioRules.InjuryReduction(physios) * 100:0}%", PhysioRules.InjuryReductionLabel(physios));
    }

    [Theory]
    [InlineData(20, 0, 20)]
    [InlineData(20, 1, 17)]
    [InlineData(20, 2, 15)]
    [InlineData(20, 3, 14)]
    [InlineData(60, 3, 42)]
    public void ReducedDays_AppliesTheReductionForTheTier(int baseDays, int physios, int expected)
    {
        Assert.Equal(expected, PhysioRules.ReducedDays(baseDays, physios));
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(1, 1)]
    [InlineData(0, 0)]
    public void ReducedDays_NeverDropsBelowOneDay(int baseDays, int physios)
    {
        Assert.Equal(1, PhysioRules.ReducedDays(baseDays, physios));
    }

    [Fact]
    public void ReducedDays_NeverMakesAnInjuryLonger()
    {
        for (var days = 1; days <= 120; days++)
            for (var physios = 0; physios <= PhysioRules.MaxPhysios; physios++)
                Assert.InRange(PhysioRules.ReducedDays(days, physios), 1, days);
    }

    [Fact]
    public void ReducedDays_MorePhysiosNeverMeansALongerRecovery()
    {
        for (var days = 1; days <= 120; days++)
            for (var physios = 1; physios <= PhysioRules.MaxPhysios; physios++)
                Assert.True(PhysioRules.ReducedDays(days, physios) <= PhysioRules.ReducedDays(days, physios - 1));
    }

    [Fact]
    public void APhysioCostsTheSameAsAScout()
    {
        Assert.Equal(ScoutRules.MonthlyWage, PhysioRules.MonthlyWage);
        Assert.Equal(3, PhysioRules.MaxPhysios);
    }

    [Theory]
    [InlineData(0, 2026, 9, 15)]
    [InlineData(1, 2026, 10, 15)]
    [InlineData(4, 2027, 1, 15)]
    public void NextPaymentDate_IsOneMonthAfterTheLastMonthWorked(int monthsWorked, int y, int m, int d)
    {
        var physio = new PhysioModel { HiredDate = new DateOnly(2026, 8, 15), MonthsWorked = monthsWorked };

        Assert.Equal(new DateOnly(y, m, d), physio.NextPaymentDate);
    }

    [Fact]
    public void NextPaymentDate_ForAnEndOfMonthHireFallsOnTheLastDayOfShortMonths()
    {
        var physio = new PhysioModel { HiredDate = new DateOnly(2027, 1, 31) };

        Assert.Equal(new DateOnly(2027, 2, 28), physio.NextPaymentDate);
        physio.MonthsWorked = 2;
        Assert.Equal(new DateOnly(2027, 4, 30), physio.NextPaymentDate);
    }
}
