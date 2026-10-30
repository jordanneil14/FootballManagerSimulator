using FootballManagerSimulator.Maui.Helpers;

namespace FootballManagerSimulator.Maui.Models;

// How AI clubs decide which players to re-sign before their contracts run out on 30 June.
public static class ContractRenewalRules
{
    public const int RenewalMonth = 3;
    public const int RenewalDay = 30;

    public const int FirstTeamSize = 11;

    // Young players within this much of the squad's strength are kept as prospects.
    public const int ProspectRatingRange = 10;

    // Clubs keep the first team and rotation almost without fail and squad players usually, but let half
    // their surplus (rank 26 and below) go, which keeps room under the 30-player limit for signings.
    // squadRank is the player's place in his squad by rating, 1 being the best; squadStrength its best-20
    // average. Age counts against fringe players far more than against first-team regulars, and young
    // players close to the squad's level are kept as prospects.
    public static double RenewalChance(PlayerModel player, int squadRank, double squadStrength, DateOnly seasonEnd)
    {
        if (player.IsRetiring(seasonEnd))
            return 0;

        var firstTeam = squadRank <= FirstTeamSize;
        var chance = squadRank switch
        {
            <= 18 => 0.98,
            <= 25 => 0.95,
            _ => 0.5
        };

        var age = player.GetAge(seasonEnd);
        if (age <= 23) chance += squadStrength - player.Rating <= ProspectRatingRange ? 0.15 : 0;
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
