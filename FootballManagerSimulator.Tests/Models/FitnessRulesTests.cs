using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class FitnessRulesTests
{
    [Fact]
    public void AnAverageStaminaPlayerLoses37OverNinetyMinutes()
    {
        Assert.Equal(37, FitnessRules.Drain(FitnessRules.StaminaReference, 90, 0), 6);
    }

    [Fact]
    public void GoalkeepersTireAtAThirdOfTheRate()
    {
        Assert.Equal(37.0 / 3, FitnessRules.Drain(FitnessRules.StaminaReference, 90, 0, FitnessRules.GoalkeeperDrainMultiplier), 6);
        Assert.Equal(FitnessRules.Drain(45, 90, 90) / 3, FitnessRules.Drain(45, 90, 90, FitnessRules.GoalkeeperDrainMultiplier), 6);
    }

    [Fact]
    public void CentreBacksTireAThirdSlower()
    {
        Assert.Equal(37.0 * 2 / 3, FitnessRules.Drain(FitnessRules.StaminaReference, 90, 0, FitnessRules.CentreBackDrainMultiplier), 6);
        Assert.Equal(FitnessRules.Drain(45, 90, 90) * 2 / 3, FitnessRules.Drain(45, 90, 90, FitnessRules.CentreBackDrainMultiplier), 6);
    }

    [Theory]
    [InlineData("GK", true)]
    [InlineData("GK/CB", true)]
    [InlineData("CB", false)]
    [InlineData("", false)]
    public void IsGoalkeeper_GoesByListedPositions(string positions, bool expected)
    {
        Assert.Equal(expected, FitnessRules.IsGoalkeeper(new PlayerModel { PreferredPosition = positions }));
    }

    [Theory]
    [InlineData("GK", FitnessRules.GoalkeeperDrainMultiplier)]
    [InlineData("GK/CB", FitnessRules.GoalkeeperDrainMultiplier)]
    [InlineData("CB", FitnessRules.CentreBackDrainMultiplier)]
    [InlineData("RB/CB", FitnessRules.CentreBackDrainMultiplier)]
    [InlineData("CDM", 1.0)]
    [InlineData("", 1.0)]
    public void RoleDrainMultiplier_GoesByListedPositions(string positions, double expected)
    {
        Assert.Equal(expected, FitnessRules.RoleDrainMultiplier(new PlayerModel { PreferredPosition = positions }), 6);
    }

    [Fact]
    public void LowStaminaTiresQuicker()
    {
        var low = FitnessRules.Drain(45, 90, 0);
        var average = FitnessRules.Drain(65, 90, 0);
        var high = FitnessRules.Drain(85, 90, 0);

        Assert.True(low > average);
        Assert.True(average > high);
    }

    [Theory]
    [InlineData(5, FitnessRules.MaxDrainFactor)]
    [InlineData(80, 0.75)]
    [InlineData(200, FitnessRules.MinDrainFactor)]
    public void DrainFactor_IsKeptWithinLimits(double stamina, double expected)
    {
        Assert.Equal(expected, FitnessRules.DrainFactor(stamina), 6);
    }

    [Fact]
    public void PressingHighDrainsAQuarterFasterForThoseMinutes()
    {
        var notPressing = FitnessRules.Drain(65, 90, 0);
        var pressingAllGame = FitnessRules.Drain(65, 90, 90);
        var pressingHalf = FitnessRules.Drain(65, 90, 45);

        Assert.Equal(notPressing * FitnessRules.HighPressDrainMultiplier, pressingAllGame, 6);
        Assert.Equal(FitnessRules.DrainPerMinute * (45 + 45 * FitnessRules.HighPressDrainMultiplier), pressingHalf, 6);
        // Pressed minutes can't exceed the minutes played.
        Assert.Equal(pressingAllGame, FitnessRules.Drain(65, 90, 500), 6);
    }

    [Fact]
    public void Recovery_IsFasterWithStaminaAndPhysios()
    {
        Assert.Equal(FitnessRules.RecoveryPerDay, FitnessRules.Recovery(65), 6);
        Assert.True(FitnessRules.Recovery(85) > FitnessRules.Recovery(65));
        Assert.Equal(FitnessRules.MinRecoveryPerDay, FitnessRules.Recovery(0), 6);
        Assert.Equal(FitnessRules.RecoveryPerDay * 1.15, FitnessRules.Recovery(65, physioCount: 1), 6);
        Assert.Equal(FitnessRules.Max, FitnessRules.Recover(97, 65));
    }

    [Theory]
    [InlineData(100, 1.0)]
    [InlineData(90, 1.0)]
    [InlineData(80, 0.94)]
    [InlineData(40, 0.7)]
    [InlineData(0, FitnessRules.MinStrengthFactor)]
    public void StrengthFactor_FallsBelowNinety(double fitness, double expected)
    {
        Assert.Equal(expected, FitnessRules.StrengthFactor(fitness), 6);
    }

    [Theory]
    [InlineData(100, 1.0)]
    [InlineData(80, 1.3)]
    [InlineData(0, 2.5)]
    public void InjuryWeight_RisesAsAPlayerTires(double fitness, double expected)
    {
        Assert.Equal(expected, FitnessRules.InjuryWeight(fitness), 6);
    }

    [Theory]
    [InlineData(100, "fit-good")]
    [InlineData(84.9, "fit-ok")]
    [InlineData(60, "fit-low")]
    [InlineData(54, "fit-poor")]
    public void CssClass_BandsByFitness(double fitness, string expected)
    {
        Assert.Equal(expected, FitnessRules.CssClass(fitness));
    }

    [Fact]
    public void NewPlayers_StartFullyFit()
    {
        Assert.Equal(FitnessRules.Max, new PlayerModel().Fitness);
    }
}
