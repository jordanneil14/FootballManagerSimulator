using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Interfaces;

public interface IPlayerHelper
{
    void AddPlayersToState(PlayerData playerData);
    int GetTransferValue(Player player);
    Player? GetPlayerById(int id);
    Player? GetPlayerByName(string name);
    bool PlayerPlaysForClub(int playerId, int clubId);
}
