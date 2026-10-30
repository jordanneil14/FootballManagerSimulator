namespace FootballManagerSimulator.Maui.Enums;

public enum CompetitionType
{
    League = 0,
    Cup = 1,
    Friendly = 2,
    EnglishPremierLeague = 3,
    EnglishFootballLeague = 4,
    EnglishCommunityShield = 5,
    EnglishNationalLeague = 6,
    ScottishPremiership = 7,
    EnglishFACup = 8
}

public static class CompetitionTypeExtensions
{
    // Ties that must produce a winner on the day, through extra time and penalties if need be.
    public static bool IsKnockout(this CompetitionType type) =>
        type is CompetitionType.Cup or CompetitionType.EnglishFACup or CompetitionType.EnglishCommunityShield;

    // English cups played across the season, whose dates the English leagues keep clear.
    public static bool IsSeasonLongEnglishCup(this CompetitionType type) =>
        type is CompetitionType.Cup or CompetitionType.EnglishFACup;
}
