using FootballManagerSimulator.Screens;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IUtils
{
    Player? GetPlayerByName(string name);
    Club? GetClubByName(string name);
    Club GetClub(int id);
    Player? GetPlayerById(int id);
    T GetResource<T>(string filename);
    void MapPlayersToAClub(List<Player.SerialisablePlayerModel> serialisablePlayers);
    int GetStartingElevenSumRatingForClub(Club club);
}
