using FootballManagerSimulator.Maui.Helpers;

namespace FootballManagerSimulator.Maui.Models;

// How AI clubs decide which players to re-sign before their contracts run out on 30 June.
public static class ContractRenewalRules
{
    public const int RenewalMonth = 3;
    public const int RenewalDay = 30;

    public const int FirstTeamSize = 11;

    // Clubs keep about 95% of a squad in its twenties: the first team and rotation almost without fail,
    // surplus players less often. squadRank is the player's place in his squad by rating, 1 being the best.
    // Age counts against fringe players far more than against first-team regulars, and young players are
    // kept as prospects.
    public static double RenewalChance(PlayerModel player, int squadRank, DateOnly seasonEnd)
    {
        if (player.IsRetiring(seasonEnd))
            return 0;

        var firstTeam = squadRank <= FirstTeamSize;
        var chance = squadRank switch
        {
            <= 18 => 0.98,
            <= 25 => 0.95,
            _ => 0.88
        };

        var age = player.GetAge(seasonEnd);
        if (age <= 23) chance += 0.15;
        else if (age >= 32) chance -= firstTeam ? 0.15 : 0.3;
        else if (age >= 30) chance -= firstTeam ? 0.05 : 0.15;

        return Math.Clamp(chance, 0.05, 0.98);
    }

    // Younger players are offered longer deals.
    public static int RenewalYears(int age) => age switch
    {
        <= 24 => RandomNumberHelper.Next(2, 5),
        <= 29 => RandomNumberHelper.Next(1, 4),
        _ => RandomNumberHelper.Next(1, 3)
    };
}
