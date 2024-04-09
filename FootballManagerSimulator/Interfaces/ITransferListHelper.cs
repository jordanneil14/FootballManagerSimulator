using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ITransferListHelper
{
    void UpdateTransferList();
    void BuyPlayerByPlayerId(int playerId);
    TransferListItem? GetTransferListItemByPlayerId(int playerId);
    bool IsPlayerTransferListed(int playerId);
    void AddPlayerToTransferList(int playerId, int askingPrice);
    void RemovePlayerFromTransferList(int playerId);
}