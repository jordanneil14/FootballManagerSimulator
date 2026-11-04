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
        Assert.Equal(SideModifiers.Neutral, TacticalModifierRules.For(With(), With(), 0, 0));
        Assert.Equal(SideModifiers.Neutral, TacticalModifierRules.For(null, null, 0, 0));
        // Being tired only matters to a side that is pressing high.
        Assert.Equal(SideModifiers.Neutral, TacticalModifierRules.For(With(), With(), 1, 1));
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
        var modifiers = TacticalModifierRules.For(With(mentality), With(), 0, 0);
        Assert.Equal(threat, modifiers.Threat, 6);
        Assert.Equal(exposure, modifiers.Exposure, 6);
        Assert.Equal(1, modifiers.CardRate);
    }

    [Fact]
    public void HighPress_FreshSideGetsTheFullEdge()
    {
        var modifiers = TacticalModifierRules.For(With(pressing: Pressing.High), With(), 0, 0);
        Assert.Equal(TacticalModifierRules.HighPressThreat, modifiers.Threat, 6);
        Assert.Equal(TacticalModifierRules.HighPressExposure, modifiers.Exposure, 6);
        Assert.Equal(TacticalModifierRules.HighPressCardRate, modifiers.CardRate);
        Assert.Equal(TacticalModifierRules.HighPressInjuryRate, modifiers.InjuryRate);
    }

    [Fact]
    public void HighPress_FullyTiredSideEndsUpWorseOff()
    {
        var modifiers = TacticalModifierRules.For(With(pressing: Pressing.High), With(), 1, 0);
        Assert.Equal(TacticalModifierRules.HighPressTiredThreat, modifiers.Threat, 6);
        Assert.Equal(TacticalModifierRules.HighPressTiredExposure, modifiers.Exposure, 6);
    }

    [Fact]
    public void HighPress_SqueezesATiredOpponentWhileItsOwnLegsAreFresh()
    {
        var vsFresh = TacticalModifierRules.For(With(pressing: Pressing.High), With(), 0, 0);
        var vsTired = TacticalModifierRules.For(With(pressing: Pressing.High), With(), 0, 1);
        var bothTired = TacticalModifierRules.For(With(pressing: Pressing.High), With(), 1, 1);

        Assert.Equal(TacticalModifierRules.HighPressThreat * (1 + TacticalModifierRules.HighPressSqueezeBonus), vsTired.Threat, 6);
        Assert.True(vsTired.Threat > vsFresh.Threat);
        Assert.Equal(TacticalModifierRules.HighPressTiredThreat, bothTired.Threat, 6); // no squeeze on tired legs
    }

    [Fact]
    public void LowPress_CostsThreatButCutsCards()
    {
        var modifiers = TacticalModifierRules.For(With(pressing: Pressing.Low), With(), 0, 0);
        Assert.Equal(TacticalModifierRules.LowPressThreat, modifiers.Threat, 6);
        Assert.Equal(TacticalModifierRules.LowPressExposure, modifiers.Exposure, 6);
        Assert.Equal(TacticalModifierRules.LowPressCardRate, modifiers.CardRate);
    }

    [Fact]
    public void LowPress_CountersATiredOpponent()
    {
        var modifiers = TacticalModifierRules.For(With(pressing: Pressing.Low), With(pressing: Pressing.High), 0, 0.5);
        Assert.Equal(TacticalModifierRules.LowPressThreat * (1 + TacticalModifierRules.LowPressCounterBonus * 0.5), modifiers.Threat, 6);
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
    public void Fatigue_ASideThatPressesFromKickOffTiresThroughTheSecondHalf()
    {
        double At(int minute) => TacticalModifierRules.FatigueProgress(TacticalModifierRules.Load(minute, minute), 50);

        Assert.Equal(0, At(45));
        Assert.Equal(0.5, At(60), 6);
        Assert.Equal(1, At(75), 6);
        Assert.Equal(1, At(110), 6);
    }

    [Fact]
    public void Fatigue_ASideThatStartsPressingLaterIsFresher()
    {
        // Pressing from the hour: by 90 it has played 90 minutes and pressed 30.
        var lateStarter = TacticalModifierRules.FatigueProgress(TacticalModifierRules.Load(90, 30), 50);
        var fromKickOff = TacticalModifierRules.FatigueProgress(TacticalModifierRules.Load(90, 90), 50);
        var neverPressed = TacticalModifierRules.FatigueProgress(TacticalModifierRules.Load(90, 0), 50);

        Assert.Equal(0.5, lateStarter, 6);
        Assert.Equal(1, fromKickOff, 6);
        Assert.Equal(0, neverPressed);
    }

    [Fact]
    public void Modifiers_Combine()
    {
        var modifiers = TacticalModifierRules.For(With(Mentality.Attacking, Pressing.High), With(), 0, 0);
        Assert.Equal(TacticalModifierRules.AttackingThreat * TacticalModifierRules.HighPressThreat, modifiers.Threat, 6);
        Assert.Equal(TacticalModifierRules.AttackingExposure * TacticalModifierRules.HighPressExposure, modifiers.Exposure, 6);
    }

    [Fact]
    public void PressingLog_CountsOnlyTheMinutesSpentPressingHigh()
    {
        var log = new List<PressingSpellModel>();
        PressingLog.Start(log, Pressing.Medium);
        PressingLog.ChangeAt(log, 30, Pressing.High);
        PressingLog.ChangeAt(log, 50, Pressing.Low);
        PressingLog.ChangeAt(log, 70, Pressing.High);

        Assert.Equal(0, PressingLog.MinutesPressedHigh(log, 30, Pressing.High));
        Assert.Equal(20, PressingLog.MinutesPressedHigh(log, 60, Pressing.High));
        Assert.Equal(40, PressingLog.MinutesPressedHigh(log, 90, Pressing.High));
    }

    [Fact]
    public void PressingLog_AChangeReplacesAnyLaterOrSameMinuteSpells()
    {
        var log = new List<PressingSpellModel>();
        PressingLog.Start(log, Pressing.Medium);
        PressingLog.ChangeAt(log, 60, Pressing.High);
        PressingLog.ChangeAt(log, 60, Pressing.Low);      // changed again in the same pause
        PressingLog.ChangeAt(log, 60, Pressing.Low);      // a line-up change: no new spell
        PressingLog.ChangeAt(log, 40, Pressing.Medium);   // replayed from earlier

        Assert.Equal([(0, Pressing.Medium)], log.Select(s => (s.FromMinute, s.Pressing)));
    }

    [Fact]
    public void PressingLog_WithNoLogAssumesTheCurrentPressingSinceKickOff()
    {
        Assert.Equal(70, PressingLog.MinutesPressedHigh([], 70, Pressing.High));
        Assert.Equal(0, PressingLog.MinutesPressedHigh([], 70, Pressing.Low));
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
