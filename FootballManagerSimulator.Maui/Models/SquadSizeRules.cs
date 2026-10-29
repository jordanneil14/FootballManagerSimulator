namespace FootballManagerSimulator.Maui.Models;

// How big AI squads should be, and how they get there.
public static class SquadSizeRules
{
    // Enough to name a starting eleven and a bench; anything short of this is topped up on 30 June.
    public const int Minimum = 18;

    // AI clubs below this sign free agents first, and will take players a little further below their level.
    public const int Target = 26;

    // AI clubs at this size sign nobody.
    public const int Maximum = 30;

    public const int Keepers = 2;

    // How far below a short squad's strength a free agent can be and still be signed (otherwise 5).
    public const int ShortSquadRatingRange = 8;
}
