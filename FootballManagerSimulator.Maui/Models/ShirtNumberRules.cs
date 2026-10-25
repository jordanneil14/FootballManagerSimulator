namespace FootballManagerSimulator.Maui.Models;

public static class ShirtNumberRules
{
    // First, second and third choice keepers.
    public static readonly int[] GoalkeeperNumbers = [1, 13, 21];

    // The traditional numbers each outfield line keeps for itself.
    public static readonly int[] DefenderNumbers = [2, 3, 5, 6];
    public static readonly int[] MidfielderNumbers = [4, 7, 8, 11];
    public static readonly int[] AttackerNumbers = [9, 10];

    private const int Max = 99;

    private static readonly HashSet<int> Reserved =
        [.. GoalkeeperNumbers, .. DefenderNumbers, .. MidfielderNumbers, .. AttackerNumbers];

    // Matches the generator and the squad colour coding: wingers and strikers are attackers.
    public static int[] ReservedNumbersFor(string position) => position.Split('/')[0].Trim().ToUpper() switch
    {
        "GK" => GoalkeeperNumbers,
        "CB" or "LCB" or "RCB" or "LB" or "RB" or "LWB" or "RWB" or "DEF" => DefenderNumbers,
        "CDM" or "LDM" or "RDM" or "CM" or "LCM" or "RCM" or "CAM" or "LAM" or "RAM" or "LM" or "RM" or "MID" => MidfielderNumbers,
        _ => AttackerNumbers
    };

    // A player first takes a free number reserved for his position, then the lowest number
    // nobody keeps (12, 14-20, 22 upwards), and only then anyone else's reserved number.
    public static int NextAvailable(IEnumerable<int> takenNumbers, string position)
    {
        var taken = takenNumbers.ToHashSet();
        var all = Enumerable.Range(1, Max);
        var unreserved = all.Where(n => !Reserved.Contains(n));

        return ReservedNumbersFor(position)
            .Concat(unreserved)
            .Concat(all)
            .FirstOrDefault(n => !taken.Contains(n));
    }

    public static int NextAvailable(IEnumerable<PlayerModel> players, int clubId, string position) =>
        NextAvailable(players
            .Where(p => p.Contract != null && p.Contract.ClubId == clubId && p.ShirtNumber.HasValue)
            .Select(p => p.ShirtNumber!.Value), position);
}
