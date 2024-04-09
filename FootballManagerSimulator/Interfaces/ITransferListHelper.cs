using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ITransferListHelper
{
    void UpdateTransferList();
    void BuyPlayerByPlayerId(int playerId);
    TransferListItem? GetTransferListItemByPlayerId(int playerId);
}