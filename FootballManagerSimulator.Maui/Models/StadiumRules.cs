namespace FootballManagerSimulator.Maui.Models;

public static class StadiumRules
{
    public const int MaxCapacity = 100_000;

    public const int UpgradeMonths = 12;

    public const double GrowthRate = 0.2;

    public const int CostPerSeat = 500;

    // Each expansion grows the stadium by a fifth, up to the maximum capacity.
    public static int SeatsAdded(int currentCapacity) =>
        Math.Max(0, Math.Min((int)(currentCapacity * GrowthRate), MaxCapacity - currentCapacity));

    // Bigger stadiums cost more to expand, since each expansion adds more seats.
    public static int UpgradeCost(int currentCapacity) => SeatsAdded(currentCapacity) * CostPerSeat;
}
