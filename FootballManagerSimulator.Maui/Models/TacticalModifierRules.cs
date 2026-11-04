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

    // Where a fully-tired pressing side ends up: the fade runs through the second half and is complete by the 75th minute.
    public const double HighPressTiredThreat = 0.88;
    public const double HighPressTiredExposure = 1.18;
    public const int FatigueStartMinute = 45;
    public const int FatigueFullMinute = 75;

    // How much of the fade a side suffers, from its outfield players' average Stamina:
    // (FatigueStaminaReference - stamina) / FatigueStaminaRange, kept between the min and 1.
    public const double FatigueStaminaReference = 85;
    public const double FatigueStaminaRange = 30;
    public const double MinFatigue = 0.3;

    // Low press: gives the opponent the ball, but no fatigue, fewer cards, and fresher late on.
    public const double LowPressThreat = 0.94;
    public const double LowPressExposure = 0.96;
    public const double LowPressCardRate = 0.80;
    public const int LowPressLateMinute = 60;
    public const double LowPressLateThreatVsHighPress = 1.10;

    public const double MinModifier = 0.5;
    public const double MaxModifier = 1.8;

    // How much of the pressing fade a side with this average outfield Stamina suffers (MinFatigue to 1).
    public static double FatigueShare(double averageStamina) =>
        Math.Clamp((FatigueStaminaReference - averageStamina) / FatigueStaminaRange, MinFatigue, 1.0);

    // How far into the fade a High-pressing side is at this minute (0 in the first half, 1 from the 75th minute).
    public static double FatigueProgress(int minute, double averageStamina)
    {
        if (minute <= FatigueStartMinute) return 0;
        var elapsed = Math.Min(1.0, (minute - FatigueStartMinute) / (double)(FatigueFullMinute - FatigueStartMinute));
        return elapsed * FatigueShare(averageStamina);
    }

    public static SideModifiers For(TeamInstructionsModel? own, TeamInstructionsModel? opponent, double ownAverageStamina, int minute)
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
                var progress = FatigueProgress(minute, ownAverageStamina);
                threat *= Lerp(HighPressThreat, HighPressTiredThreat, progress);
                exposure *= Lerp(HighPressExposure, HighPressTiredExposure, progress);
                cards *= HighPressCardRate;
                injuries *= HighPressInjuryRate;
                break;
            case Pressing.Low:
                threat *= LowPressThreat;
                exposure *= LowPressExposure;
                cards *= LowPressCardRate;
                if (minute > LowPressLateMinute && opponent.Pressing == Pressing.High)
                    threat *= LowPressLateThreatVsHighPress;
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
