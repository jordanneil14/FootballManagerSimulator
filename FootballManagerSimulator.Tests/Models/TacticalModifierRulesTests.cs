using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class TacticalModifierRulesTests
{
    private static TeamInstructionsModel With(Mentality mentality = Mentality.Balanced, Pressing pressing = Pressing.Medium) =>
        new() { Mentality = mentality, Pressing = pressing };

    [Fact]
    public void NeutralInstructions_ChangeNothing()
    {
        Assert.Equal(SideModifiers.Neutral, TacticalModifierRules.For(With(), With(), 60, 30));
        Assert.Equal(SideModifiers.Neutral, TacticalModifierRules.For(null, null, 60, 30));
    }

    [Fact]
    public void NewInstructions_DefaultToNeutral()
    {
        var instructions = new TeamInstructionsModel();
        Assert.Equal(Mentality.Balanced, instructions.Mentality);
        Assert.Equal(Pressing.Medium, instructions.Pressing);
        Assert.Equal(Mentality.Balanced, default(Mentality));
        Assert.Equal(Pressing.Medium, default(Pressing));
    }

    [Theory]
    [InlineData(Mentality.Defensive, TacticalModifierRules.DefensiveThreat, TacticalModifierRules.DefensiveExposure)]
    [InlineData(Mentality.Attacking, TacticalModifierRules.AttackingThreat, TacticalModifierRules.AttackingExposure)]
    public void Mentality_ScalesThreatAndExposure(Mentality mentality, double threat, double exposure)
    {
        var modifiers = TacticalModifierRules.For(With(mentality), With(), 60, 30);
        Assert.Equal(threat, modifiers.Threat, 6);
        Assert.Equal(exposure, modifiers.Exposure, 6);
        Assert.Equal(1, modifiers.CardRate);
    }

    [Fact]
    public void HighPress_IsAtFullStrengthInTheFirstHalf()
    {
        var modifiers = TacticalModifierRules.For(With(pressing: Pressing.High), With(), 50, 45);
        Assert.Equal(TacticalModifierRules.HighPressThreat, modifiers.Threat, 6);
        Assert.Equal(TacticalModifierRules.HighPressExposure, modifiers.Exposure, 6);
        Assert.Equal(TacticalModifierRules.HighPressCardRate, modifiers.CardRate);
        Assert.Equal(TacticalModifierRules.HighPressInjuryRate, modifiers.InjuryRate);
    }

    [Fact]
    public void HighPress_AnUnfitSideIsFullyTiredByFullTime()
    {
        var modifiers = TacticalModifierRules.For(With(pressing: Pressing.High), With(), 50, 90);
        Assert.Equal(TacticalModifierRules.HighPressTiredThreat, modifiers.Threat, 6);
        Assert.Equal(TacticalModifierRules.HighPressTiredExposure, modifiers.Exposure, 6);
    }

    [Fact]
    public void HighPress_FitSidesFadeLess()
    {
        var unfit = TacticalModifierRules.For(With(pressing: Pressing.High), With(), 50, 90);
        var fit = TacticalModifierRules.For(With(pressing: Pressing.High), With(), 85, 90);
        Assert.True(fit.Threat > unfit.Threat);
        Assert.True(fit.Exposure < unfit.Exposure);
        // Even the fittest side fades a little.
        Assert.True(fit.Threat < TacticalModifierRules.HighPressThreat);
    }

    [Theory]
    [InlineData(40, 1.0)]
    [InlineData(55, 1.0)]
    [InlineData(70, 0.5)]
    [InlineData(90, TacticalModifierRules.MinFatigue)]
    public void FatigueShare_DependsOnStamina(double stamina, double expected)
    {
        Assert.Equal(expected, TacticalModifierRules.FatigueShare(stamina), 6);
    }

    [Fact]
    public void FatigueProgress_RampsThroughTheSecondHalfAndStays()
    {
        Assert.Equal(0, TacticalModifierRules.FatigueProgress(45, 50));
        Assert.Equal(0.5, TacticalModifierRules.FatigueProgress(60, 50), 6);
        Assert.Equal(1, TacticalModifierRules.FatigueProgress(75, 50), 6);
        Assert.Equal(1, TacticalModifierRules.FatigueProgress(115, 50), 6);
    }

    [Fact]
    public void LowPress_CostsThreatButCutsCards()
    {
        var modifiers = TacticalModifierRules.For(With(pressing: Pressing.Low), With(), 60, 30);
        Assert.Equal(TacticalModifierRules.LowPressThreat, modifiers.Threat, 6);
        Assert.Equal(TacticalModifierRules.LowPressExposure, modifiers.Exposure, 6);
        Assert.Equal(TacticalModifierRules.LowPressCardRate, modifiers.CardRate);
    }

    [Fact]
    public void LowPress_IsSharperLateOnAgainstAHighPress()
    {
        var early = TacticalModifierRules.For(With(pressing: Pressing.Low), With(pressing: Pressing.High), 60, 60);
        var late = TacticalModifierRules.For(With(pressing: Pressing.Low), With(pressing: Pressing.High), 60, 61);
        var lateVsMedium = TacticalModifierRules.For(With(pressing: Pressing.Low), With(), 60, 61);

        Assert.Equal(TacticalModifierRules.LowPressThreat, early.Threat, 6);
        Assert.Equal(TacticalModifierRules.LowPressThreat * TacticalModifierRules.LowPressLateThreatVsHighPress, late.Threat, 6);
        Assert.Equal(TacticalModifierRules.LowPressThreat, lateVsMedium.Threat, 6);
    }

    [Fact]
    public void Modifiers_Combine()
    {
        var modifiers = TacticalModifierRules.For(With(Mentality.Attacking, Pressing.High), With(), 60, 30);
        Assert.Equal(TacticalModifierRules.AttackingThreat * TacticalModifierRules.HighPressThreat, modifiers.Threat, 6);
        Assert.Equal(TacticalModifierRules.AttackingExposure * TacticalModifierRules.HighPressExposure, modifiers.Exposure, 6);
    }

    [Fact]
    public void CombineWeights_WithNeutralInstructionsAreTheStrengthShares()
    {
        var weights = MatchSimulatorHelper.CombineWeights(3, 1, SideModifiers.Neutral, SideModifiers.Neutral);
        Assert.Equal(0.75, weights.Home, 6);
        Assert.Equal(0.25, weights.Away, 6);
    }

    [Fact]
    public void CombineWeights_ThreatAndExposureScaleEachSide()
    {
        var home = new SideModifiers(1.2, 1.1, 1, 1);
        var away = new SideModifiers(0.9, 0.8, 1, 1);
        var weights = MatchSimulatorHelper.CombineWeights(1, 1, home, away);
        Assert.Equal(0.5 * 1.2 * 0.8, weights.Home, 6);
        Assert.Equal(0.5 * 0.9 * 1.1, weights.Away, 6);
    }

    [Theory]
    [InlineData(0.70, 75, Mentality.Attacking, Pressing.High)]
    [InlineData(0.70, 60, Mentality.Attacking, Pressing.Medium)]
    [InlineData(0.50, 60, Mentality.Balanced, Pressing.Medium)]
    [InlineData(0.50, 80, Mentality.Balanced, Pressing.High)]
    [InlineData(0.30, 80, Mentality.Defensive, Pressing.Low)]
    public void AI_SetsUpForItsChances(double share, double stamina, Mentality mentality, Pressing pressing)
    {
        var instructions = MatchSimulatorHelper.PickAIInstructions(share, stamina, softenRoll: 0.99);
        Assert.Equal(mentality, instructions.Mentality);
        Assert.Equal(pressing, instructions.Pressing);
    }

    [Fact]
    public void AI_SometimesSoftensItsMentality()
    {
        Assert.Equal(Mentality.Balanced, MatchSimulatorHelper.PickAIInstructions(0.7, 60, softenRoll: 0.0).Mentality);
        Assert.Equal(Mentality.Balanced, MatchSimulatorHelper.PickAIInstructions(0.3, 60, softenRoll: 0.0).Mentality);
    }
}
