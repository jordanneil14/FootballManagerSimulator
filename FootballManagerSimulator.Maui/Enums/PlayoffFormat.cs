namespace FootballManagerSimulator.Maui.Enums;

// How a league's play-off places compete for its last promotion place.
public enum PlayoffFormat
{
    // Four clubs: two-legged semi-finals (top v bottom) and a one-off final.
    TwoLeggedSemiFinals = 0,

    // Six clubs: 5th v 8th and 6th v 7th in one-off preliminaries, then two-legged semi-finals
    // (3rd and 4th waiting) and a two-legged final. A level tie goes to the higher-placed club.
    SerieB = 1,

    // Six clubs: one-off eliminators between the bottom four, one-off semi-finals at the top two's
    // grounds and a one-off final, each going to extra time and penalties if level.
    NationalLeague = 2,

    // The play-off place faces the club just above the relegation places in the league above,
    // over two legs; the winner plays in the league above next season.
    RelegationPlayoff = 3
}
