using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ITransferListHelper
{
    void UpdateTransferList();
    void TransferContractedPlayerByPlayerIdAndClubId(int playerId, int clubId);
    TransferListItem? GetTransferListItemByPlayerId(int playerId);
    bool IsPlayerTransferListed(int playerId);
    void AddPlayerToTransferList(int playerId, int askingPrice);
    void RemovePlayerFromTransferList(int playerId);
    void SignFreeAgentByPlayerId(int playerId);
    void ProcessAITransfers();
}