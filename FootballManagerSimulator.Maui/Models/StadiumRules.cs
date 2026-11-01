namespace FootballManagerSimulator.Maui.Models;

public static class StadiumRules
{
    public const int MaxCapacity = 100_000;

    // The smallest a part-tier expansion can cost.
    public const int MinUpgradeCost = 100_000;

    // Cost and Months are what it takes to build up to this tier from the one below.
    public record Tier(int Capacity, int Cost, int Months);

    // Each expansion takes the stadium up to the next tier. The first tier is where the ladder
    // starts, so there's nothing to build to reach it.
    public static readonly IReadOnlyList<Tier> Tiers =
    [
        new(5_000, 0, 0),
        new(10_000, 750_000, 6),
        new(15_000, 1_500_000, 6),
        new(20_000, 3_000_000, 9),
        new(30_000, 6_000_000, 12),
        new(40_000, 10_000_000, 12),
        new(50_000, 15_000_000, 15),
        new(60_000, 22_000_000, 18),
        new(75_000, 35_000_000, 18),
        new(90_000, 50_000_000, 24),
        new(MaxCapacity, 70_000_000, 24)
    ];

    // The tier the stadium has reached (1-based). Stadiums smaller than the first tier count as tier 1.
    public static int TierNumber(int currentCapacity) =>
        Math.Max(1, Tiers.Count(p => p.Capacity <= currentCapacity));

    // The tier the next expansion builds to, or null once the stadium is at the maximum capacity.
    public static Tier? NextTier(int currentCapacity) =>
        Tiers.Skip(1).FirstOrDefault(p => p.Capacity > currentCapacity);

    public static int SeatsAdded(int currentCapacity) =>
        NextTier(currentCapacity) is { } next ? next.Capacity - currentCapacity : 0;

    public static int UpgradeMonths(int currentCapacity) =>
        NextTier(currentCapacity)?.Months ?? 0;

    // A stadium part-way through a tier pays only for the share of the tier it still has to build,
    // so a club at 19,900 seats doesn't pay the full price for 100 seats. Stadiums below the first
    // tier pay the full price.
    public static int UpgradeCost(int currentCapacity)
    {
        var nextIndex = Tiers.Skip(1).TakeWhile(p => p.Capacity <= currentCapacity).Count() + 1;
        if (nextIndex >= Tiers.Count) return 0;

        var next = Tiers[nextIndex];
        var previous = Tiers[nextIndex - 1].Capacity;
        var share = Math.Min(1.0, (double)(next.Capacity - currentCapacity) / (next.Capacity - previous));
        var cost = Math.Round(next.Cost * share / 10_000) * 10_000;
        return (int)Math.Max(MinUpgradeCost, cost);
    }
}
