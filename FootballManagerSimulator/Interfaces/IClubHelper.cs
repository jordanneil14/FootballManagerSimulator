using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IClubHelper
{
    Club? GetClubByName(string name);
    Club GetClubById(int id);
    int GetStartingElevenSumRatingForClub(Club club);
}
