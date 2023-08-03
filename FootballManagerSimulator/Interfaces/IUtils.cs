using FootballManagerSimulator.Screens;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IUtils
{
    Player? GetPlayerByName(string name);
    Player? GetPlayerByClubAndShirtNumber(Club club, int shirtNumber);
    Club? GetTeamByName(string name);
    Club GetTeam(int id);
    Player GetPlayer(int id);
    T GetResource<T>(string filename);
    void MapPlayersToATeam(List<Player.SerialisablePlayerModel> serialisablePlayers);
}
