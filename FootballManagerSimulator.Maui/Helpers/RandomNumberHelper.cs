namespace FootballManagerSimulator.Maui.Helpers;

public static class RandomNumberHelper
{
    public static int Next() => Random.Shared.Next();

    public static int Next(int maxValue) => Random.Shared.Next(maxValue);

    public static int Next(int minValue, int maxValue) => Random.Shared.Next(minValue, maxValue);

    public static double NextDouble() => Random.Shared.NextDouble();
}