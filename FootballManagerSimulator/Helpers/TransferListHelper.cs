using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Helpers;

public class TransferListHelper : ITransferListHelper
{
    private readonly IState State;
    private readonly IPlayerHelper PlayerHelper;

    public TransferListHelper(
        IState state,
        IPlayerHelper playerHelper)
    {
        State = state;
        PlayerHelper = playerHelper;
    }

    public void BuyPlayerByPlayerId(int playerId)
    {
        var transferListItem = State.TransferListItems.First(p => p.PlayerId == playerId);
        State.Players.First(p => p.Id == playerId).Contract!.ClubId = State.MyClub.Id;
        State.TransferListItems.Remove(transferListItem);
        State.MyClub.TransferBudget -= transferListItem.AskingPrice;

        // Add player to reserve tactic slot
        State.Clubs
            .First(p => p.Id == State.MyClub.Id)
            .TacticSlots
            .First(p => p.TacticSlotType == Enums.TacticSlotType.RES && p.PlayerId == null)
            .PlayerId = transferListItem.PlayerId;
    }

    public TransferListItem? GetTransferListItemByPlayerId(int playerId)
    {
        return State.TransferListItems.FirstOrDefault(p => p.PlayerId == playerId);
    }

    public void UpdateTransferList()
    {
        State.TransferListItems.Clear();

        foreach (var league in State.Leagues)
        {
            var clubIds = league.Clubs.Select(p => p.Id);
            var players = State.Players.Where(p => p.Contract != null && clubIds.Contains(p.Contract.ClubId));
            var randomPlayers = players.OrderBy(p => RandomNumberHelper.Next()).Take(20);

            var transferListItems = randomPlayers.Select(p =>
            {
                var transferValue = PlayerHelper.GetTransferValue(p);
                var askingPrice = RandomNumberHelper.Next(transferValue, (int)(transferValue * 1.5));
                return new TransferListItem
                {
                    PlayerId = p.Id,
                    AskingPrice = askingPrice
                };
            });

            State.TransferListItems.AddRange(transferListItems);
        }
    }
}
