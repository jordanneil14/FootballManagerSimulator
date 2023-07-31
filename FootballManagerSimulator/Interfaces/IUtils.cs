using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IUtils
{
    Player? GetPlayerByName(string name);
    Team? GetTeamByName(string name);
    Team GetTeam(int id);
    Player GetPlayer(int id);
    T GetResource<T>(string filename);
}
