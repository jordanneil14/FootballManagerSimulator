namespace FootballManagerSimulator.Maui.Models;

// Player fitness (0-100). It drops every minute a player is on the pitch, faster with low Stamina
// and while his side presses high, and recovers every day. A tired player counts for less in the
// team's strength and is more likely to get injured.
public static class FitnessRules
{
    public const double Max = 100;

    // Drain per minute on the pitch for a player of reference Stamina: 37 over 90 minutes.
    public const double DrainPerMinute = 37.0 / 90;
    public const double StaminaReference = 65;
    // Each Stamina point below (or above) the reference drains 1/StaminaRange faster (or slower).
    public const double StaminaRange = 60;
    public const double MinDrainFactor = 0.6;
    public const double MaxDrainFactor = 1.6;
    public const double HighPressDrainMultiplier = 1.25;
    // Goalkeepers cover far less ground, so they tire at a third of the rate (two-thirds less).
    public const double GoalkeeperDrainMultiplier = 1.0 / 3;

    // Daily recovery for a player of reference Stamina, plus this much per Stamina point above it.
    public const double RecoveryPerDay = 8;
    public const double RecoveryPerStaminaPoint = 0.1;
    public const double MinRecoveryPerDay = 4;

    // Strength: full at FullStrengthFitness and above, then this much less per point below it.
    public const double FullStrengthFitness = 90;
    public const double StrengthLossPerPoint = 0.006;
    public const double MinStrengthFactor = 0.6;

    // Injury weight: a fully fit player 1, rising by this much per 100 points of lost fitness.
    public const double TiredInjuryWeight = 1.5;

    // How much faster than average this player tires (MinDrainFactor to MaxDrainFactor).
    public static double DrainFactor(double stamina) =>
        Math.Clamp(1 + (StaminaReference - stamina) / StaminaRange, MinDrainFactor, MaxDrainFactor);

    // Fitness lost over minutesOnPitch, pressedHighMinutes of which his side spent pressing high.
    public static double Drain(double stamina, int minutesOnPitch, int pressedHighMinutes, bool goalkeeper = false)
    {
        var pressed = Math.Clamp(pressedHighMinutes, 0, Math.Max(0, minutesOnPitch));
        var effectiveMinutes = minutesOnPitch + (HighPressDrainMultiplier - 1) * pressed;
        var role = goalkeeper ? GoalkeeperDrainMultiplier : 1;
        return Math.Max(0, DrainPerMinute * DrainFactor(stamina) * effectiveMinutes * role);
    }

    // A keeper by trade (GK among his listed positions) tires at the goalkeeper rate wherever he plays.
    public static bool IsGoalkeeper(PlayerModel player) =>
        player.PreferredPosition.Split('/', StringSplitOptions.TrimEntries).Contains("GK");

    // Fitness regained in a day. Physios speed it up by the same share they cut injury lay-offs.
    public static double Recovery(double stamina, int physioCount = 0) =>
        Math.Max(MinRecoveryPerDay, RecoveryPerDay + (stamina - StaminaReference) * RecoveryPerStaminaPoint)
        * (1 + PhysioRules.InjuryReduction(physioCount));

    public static double Recover(double fitness, double stamina, int physioCount = 0) =>
        Math.Min(Max, fitness + Recovery(stamina, physioCount));

    // What a player at this fitness is worth compared with when fully fit.
    public static double StrengthFactor(double fitness) =>
        Math.Clamp(1 - StrengthLossPerPoint * Math.Max(0, FullStrengthFitness - fitness), MinStrengthFactor, 1);

    // How likely a player at this fitness is to be the one injured, compared with a fully fit one.
    public static double InjuryWeight(double fitness) =>
        1 + TiredInjuryWeight * Math.Clamp(Max - fitness, 0, Max) / Max;

    // Colour band for the fitness bar.
    public static string CssClass(double fitness) => fitness switch
    {
        >= 85 => "fit-good",
        >= 70 => "fit-ok",
        >= 55 => "fit-low",
        _ => "fit-poor"
    };
}
