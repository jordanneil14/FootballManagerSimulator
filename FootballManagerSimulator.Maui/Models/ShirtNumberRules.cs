namespace FootballManagerSimulator.Maui.Models;

public static class ShirtNumberRules
{
    // First, second and third choice keepers.
    public static readonly int[] GoalkeeperNumbers = [1, 13, 21];

    private const int Max = 99;

    // Lowest free number first. Keepers take 1, 13 then 21 and any extra keepers go from 22 upwards;
    // outfield players never take a keeper's number while another is free.
    public static int NextAvailable(IEnumerable<int> takenNumbers, string position)
    {
        var taken = takenNumbers.ToHashSet();
        var all = Enumerable.Range(1, Max);

        var preferred = position == "GK"
            ? GoalkeeperNumbers.Concat(Enumerable.Range(22, Max - 21))
            : all.Except(GoalkeeperNumbers);

        return preferred.Concat(all).FirstOrDefault(n => !taken.Contains(n));
    }

    public static int NextAvailable(IEnumerable<PlayerModel> players, int clubId, string position) =>
        NextAvailable(players
            .Where(p => p.Contract != null && p.Contract.ClubId == clubId && p.ShirtNumber.HasValue)
            .Select(p => p.ShirtNumber!.Value), position);
}
