using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IPlayerHelper
{
    void AddPlayersToState(PlayerData playerData);
    Player? GetPlayerById(int id);
    Player? GetPlayerByName(string name);
}
