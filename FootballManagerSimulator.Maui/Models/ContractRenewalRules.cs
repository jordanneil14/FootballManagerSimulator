using FootballManagerSimulator.Maui.Helpers;

namespace FootballManagerSimulator.Maui.Models;

// How AI clubs decide which players to re-sign before their contracts run out on 30 June.
public static class ContractRenewalRules
{
    public const int RenewalMonth = 3;
    public const int RenewalDay = 30;

    // Clubs keep most players, favouring those at or above the squad's level and the young,
    // and are reluctant to commit to players in their thirties.
    public static double RenewalChance(PlayerModel player, double squadAverageRating, DateOnly seasonEnd)
    {
        if (player.IsRetiring(seasonEnd))
            return 0;

        var age = player.GetAge(seasonEnd);
        var chance = 0.55;
        if (player.Rating >= squadAverageRating) chance += 0.3;
        if (age <= 23) chance += 0.15;
        if (age >= 32) chance -= 0.3;
        else if (age >= 30) chance -= 0.15;

        return Math.Clamp(chance, 0.1, 0.95);
    }

    // Younger players are offered longer deals.
    public static int RenewalYears(int age) => age switch
    {
        <= 24 => RandomNumberHelper.Next(2, 5),
        <= 29 => RandomNumberHelper.Next(1, 4),
        _ => RandomNumberHelper.Next(1, 3)
    };
}
