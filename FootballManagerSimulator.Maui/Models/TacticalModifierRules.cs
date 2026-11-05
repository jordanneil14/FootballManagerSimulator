namespace FootballManagerSimulator.Maui.Models;

// What a side's team instructions do to a match at a given minute.
//   Threat:   how dangerous the side is going forward (1 = neutral).
//   Exposure: how open it is at the back (1 = neutral).
//   CardRate / InjuryRate: how often its players are booked or injured (1 = neutral).
// A side's weight to score is its strength share × its Threat × the opponent's Exposure, so
// Threat = Exposure changes how many goals there are but not who scores them (a risk dial),
// and Threat > Exposure is a real edge that has to be paid for some other way.
public readonly record struct SideModifiers(double Threat, double Exposure, double CardRate, double InjuryRate)
{
    public static readonly SideModifiers Neutral = new(1, 1, 1, 1);
}

public static class TacticalModifierRules
{
    // Mentality: mostly a risk dial. Fewer goals helps an underdog (more draws and upsets);
    // more goals lets a stronger side's quality show.
    public const double DefensiveThreat = 0.82;
    public const double DefensiveExposure = 0.80;
    public const double AttackingThreat = 1.18;
    public const double AttackingExposure = 1.22;

    // High press: an edge while the legs last, paid for in cards, injuries and a late fade.
    public const double HighPressThreat = 1.08;
    public const double HighPressExposure = 0.97;
    public const double HighPressCardRate = 1.35;
    public const double HighPressInjuryRate = 1.20;

    // Where a fully-tired pressing side ends up. Softer than it would otherwise be because player
    // fitness (FitnessRules) also drains faster while pressing, so tiredness isn't counted twice.
    public const double HighPressTiredThreat = 0.92;
    public const double HighPressTiredExposure = 1.12;

    // Fatigue comes from a side's workload: every minute played counts once, and every minute spent
    // pressing high counts PressingLoadFactor more. A side pressing from kick-off starts to tire at
    // half-time (load 90) and is fully tired by the 75th minute (load 150). A side that only starts
    // pressing later is fresher, so it tires later; one that never presses never gets there in 90 minutes.
    public const double PressingLoadFactor = 1.0;
    public const double FatigueStartLoad = 90;
    public const double FatigueFullLoad = 150;

    // Pressing or sitting off against a side that is tiring: its threat goes up by this much
    // times how tired the opponent is (0 to 1). Squeezing them works only with fresh legs of your own.
    public const double HighPressSqueezeBonus = 0.15;
    public const double LowPressCounterBonus = 0.25;

    // How much of the fade a side suffers, from its outfield players' average Stamina:
    // (FatigueStaminaReference - stamina) / FatigueStaminaRange, kept between the min and 1.
    public const double FatigueStaminaReference = 85;
    public const double FatigueStaminaRange = 30;
    public const double MinFatigue = 0.3;

    // Low press: gives the opponent the ball, but players tire a little slower, fewer cards, and dangerous on the
    // counter against a side that has run itself into the ground.
    public const double LowPressThreat = 0.94;
    public const double LowPressExposure = 0.96;
    public const double LowPressCardRate = 0.80;

    public const double MinModifier = 0.5;
    public const double MaxModifier = 1.8;

    // How much of the pressing fade a side with this average outfield Stamina suffers (MinFatigue to 1).
    public static double FatigueShare(double averageStamina) =>
        Math.Clamp((FatigueStaminaReference - averageStamina) / FatigueStaminaRange, MinFatigue, 1.0);

    // A side's workload by this minute, having pressed high for minutesPressedHigh of it.
    public static double Load(int minute, int minutesPressedHigh) => minute + PressingLoadFactor * minutesPressedHigh;

    // How tired a side is (0 = fresh, 1 = fully tired), from its workload and Stamina.
    public static double FatigueProgress(double load, double averageStamina)
    {
        if (load <= FatigueStartLoad) return 0;
        var elapsed = Math.Min(1.0, (load - FatigueStartLoad) / (FatigueFullLoad - FatigueStartLoad));
        return elapsed * FatigueShare(averageStamina);
    }

    // ownFatigue / opponentFatigue: each side's FatigueProgress at this minute.
    public static SideModifiers For(TeamInstructionsModel? own, TeamInstructionsModel? opponent, double ownFatigue, double opponentFatigue)
    {
        own ??= new TeamInstructionsModel();
        opponent ??= new TeamInstructionsModel();

        double threat = 1, exposure = 1, cards = 1, injuries = 1;

        switch (own.Mentality)
        {
            case Mentality.Defensive:
                threat *= DefensiveThreat;
                exposure *= DefensiveExposure;
                break;
            case Mentality.Attacking:
                threat *= AttackingThreat;
                exposure *= AttackingExposure;
                break;
        }

        switch (own.Pressing)
        {
            case Pressing.High:
                threat *= Lerp(HighPressThreat, HighPressTiredThreat, ownFatigue);
                exposure *= Lerp(HighPressExposure, HighPressTiredExposure, ownFatigue);
                // Squeezing a tired side: they can't play out under pressure, as long as your own legs are fresh.
                threat *= 1 + HighPressSqueezeBonus * opponentFatigue * (1 - ownFatigue);
                cards *= HighPressCardRate;
                injuries *= HighPressInjuryRate;
                break;
            case Pressing.Low:
                threat *= LowPressThreat;
                exposure *= LowPressExposure;
                cards *= LowPressCardRate;
                // Sitting off a tired side: the space behind them opens up on the counter.
                threat *= 1 + LowPressCounterBonus * opponentFatigue;
                break;
        }

        return new SideModifiers(
            Math.Clamp(threat, MinModifier, MaxModifier),
            Math.Clamp(exposure, MinModifier, MaxModifier),
            cards,
            injuries);
    }

    private static double Lerp(double from, double to, double t) => from + (to - from) * t;
}
