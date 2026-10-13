using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Interfaces;

public interface IPlayerHelper
{
    void AddPlayersToState(PlayerData playerData);
    int GetTransferValue(PlayerModel player);
    PlayerModel? GetPlayerById(int id);
    PlayerModel? GetPlayerByName(string name);
    bool DoesPlayerPlaysForClub(int playerId, int clubId);
}
