namespace FootballManagerSimulator.Maui.Models;

public static class TransferListRules
{
    // Up to this multiple of a player's value, clubs bid as normal.
    public const double FullInterestUpTo = 1.2;

    // Beyond this multiple, bids are rare.
    public const double RareInterestFrom = 2.0;

    public const double RareBidChance = 0.02;

    // The chance a suitable club bids for a listed player, falling away as the asking price
    // climbs above the player's value so a player can't be sold for any price a budget covers.
    public static double BidChance(int askingPrice, int transferValue)
    {
        if (transferValue <= 0)
            return 1;

        var ratio = (double)askingPrice / transferValue;
        if (ratio <= FullInterestUpTo)
            return 1;
        if (ratio >= RareInterestFrom)
            return RareBidChance;

        var progress = (ratio - FullInterestUpTo) / (RareInterestFrom - FullInterestUpTo);
        return 1 - progress * (1 - RareBidChance);
    }
}
