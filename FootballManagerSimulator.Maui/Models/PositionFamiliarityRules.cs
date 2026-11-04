using FootballManagerSimulator.Maui.Enums;

namespace FootballManagerSimulator.Maui.Models;

public enum PositionFamiliarity
{
    Natural,
    Adjacent,
    Related,
    Unfamiliar,
    GoalkeeperSwap
}

// How well a player knows the slot he's picked in, and what that does to his rating on match day.
// A player's displayed Rating never changes; only what he adds to the starting XI does.
public static class PositionFamiliarityRules
{
    public const double AdjacentMultiplier = 0.95;
    public const double RelatedMultiplier = 0.85;
    public const double UnfamiliarMultiplier = 0.70;
    public const double GoalkeeperSwapMultiplier = 0.40;

    // Neighbouring roles with a very similar job.
    private static readonly HashSet<(string, string)> AdjacentPairs = BothWays(
    [
        ("CB", "CDM"), ("CDM", "CM"), ("CM", "CAM"), ("CAM", "ST"),
        ("LB", "LM"), ("RB", "RM"), ("LM", "LW"), ("RM", "RW"),
        ("LW", "ST"), ("RW", "ST")
    ]);

    // Same line or same flank, different job.
    private static readonly HashSet<(string, string)> RelatedPairs = BothWays(
    [
        ("CB", "LB"), ("CB", "RB"), ("LB", "RB"),
        ("CDM", "CAM"),
        ("CM", "LM"), ("CM", "RM"), ("LM", "RM"),
        ("LB", "LW"), ("RB", "RW"),
        ("LW", "RW"),
        ("CAM", "LW"), ("CAM", "RW")
    ]);

    private static readonly HashSet<string> KnownPositions =
        ["GK", "CB", "LB", "RB", "CDM", "CM", "CAM", "LM", "RM", "LW", "RW", "ST"];

    // The best familiarity across every position the player lists. Every listed position is natural.
    public static PositionFamiliarity GetFamiliarity(string preferredPositions, TacticSlotType slot)
    {
        if (slot is TacticSlotType.SUB or TacticSlotType.RES)
            return PositionFamiliarity.Natural;

        var positions = (preferredPositions ?? "")
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalise)
            .Where(KnownPositions.Contains)
            .ToList();

        // Old saves and hand-made players with no usable position aren't penalised.
        if (positions.Count == 0)
            return PositionFamiliarity.Natural;

        var slotPosition = Normalise(slot.ToString());
        return positions.Select(p => Compare(p, slotPosition)).Min();
    }

    public static PositionFamiliarity GetFamiliarity(PlayerModel player, TacticSlotType slot) =>
        GetFamiliarity(player.PreferredPosition, slot);

    public static double Multiplier(PositionFamiliarity familiarity) => familiarity switch
    {
        PositionFamiliarity.Natural => 1.0,
        PositionFamiliarity.Adjacent => AdjacentMultiplier,
        PositionFamiliarity.Related => RelatedMultiplier,
        PositionFamiliarity.Unfamiliar => UnfamiliarMultiplier,
        PositionFamiliarity.GoalkeeperSwap => GoalkeeperSwapMultiplier,
        _ => 1.0
    };

    public static int EffectiveRating(PlayerModel player, TacticSlotType slot) =>
        (int)Math.Round(player.Rating * Multiplier(GetFamiliarity(player, slot)), MidpointRounding.AwayFromZero);

    private static PositionFamiliarity Compare(string position, string slot)
    {
        if (position == slot) return PositionFamiliarity.Natural;
        if (position == "GK" || slot == "GK") return PositionFamiliarity.GoalkeeperSwap;
        if (AdjacentPairs.Contains((position, slot))) return PositionFamiliarity.Adjacent;
        if (RelatedPairs.Contains((position, slot))) return PositionFamiliarity.Related;
        return PositionFamiliarity.Unfamiliar;
    }

    // LS and LF are generator labels for a striker.
    private static string Normalise(string position) => position switch
    {
        "LS" or "LF" or "RS" or "RF" or "CF" => "ST",
        _ => position
    };

    private static HashSet<(string, string)> BothWays(IEnumerable<(string A, string B)> pairs) =>
        pairs.SelectMany(p => new[] { (p.A, p.B), (p.B, p.A) }).ToHashSet();
}
