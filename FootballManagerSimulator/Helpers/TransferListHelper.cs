using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Helpers;

public class TransferListHelper(
    IState state,
    IPlayerHelper playerHelper,
    IClubHelper clubHelper,
    INotificationFactory notificationFactory) : ITransferListHelper
{
    public void AddPlayerToTransferList(int playerId, int askingPrice)
    {
        state.TransferListItems.Add(new TransferListItem
        {
            PlayerId = playerId,
            AskingPrice = askingPrice,
            ClubId = state.Players.First(p => p.Id == playerId).Contract!.ClubId
        });
    }

    private DateOnly GetEndOfCurrentSeasonDate()
    {
        var year = state.Date.Month < 7 ? state.Date.Year : state.Date.Year + 1;
        var date = new DateOnly(year, 6, 30);
        return date;
    }

    public void SignFreeAgentByPlayerId(int playerId)
    {
        state.Players.First(p => p.Id == playerId).Contract = new Player.ContractModel
        {
            ClubId = state.MyClub.Id,
            ClubName = state.MyClub.Name,
            ExpiryDate = GetEndOfCurrentSeasonDate()
        };

        // Add player to reserve tactic slot
        state.Clubs
            .First(p => p.Id == state.MyClub.Id)
            .TacticSlots
            .First(p => p.TacticSlotType == Enums.TacticSlotType.RES && p.PlayerId == null)
            .PlayerId = playerId;
    }

    public void TransferContractedPlayerByPlayerIdAndClubId(int playerId, int clubId)
    {
        var transferListItem = state.TransferListItems.First(p => p.PlayerId == playerId);
        state.Players.First(p => p.Id == playerId).Contract!.ClubId = clubId;
        state.TransferListItems.Remove(transferListItem);
        state.Clubs.First(p => p.Id == clubId).TransferBudget -= transferListItem.AskingPrice;

        // Add player to reserve tactic slot
        state.Clubs
            .First(p => p.Id == clubId)
            .TacticSlots
            .First(p => p.TacticSlotType == Enums.TacticSlotType.RES && p.PlayerId == null)
            .PlayerId = transferListItem.PlayerId;
    }

    public TransferListItem? GetTransferListItemByPlayerId(int playerId)
    {
        return state.TransferListItems.FirstOrDefault(p => p.PlayerId == playerId);
    }

    public bool IsPlayerTransferListed(int playerId)
    {
        return state.TransferListItems.Any(p => p.PlayerId == playerId);
    }

    public void RemovePlayerFromTransferList(int playerId)
    {
        state.TransferListItems.RemoveAll(p => p.PlayerId == playerId);
    }

    public void UpdateTransferList()
    {
        state.TransferListItems.Clear();

        foreach (var league in state.Competitions)
        {
            var clubIds = league.Clubs.Select(p => p.Id);
            var players = state.Players.Where(p => p.Contract != null && clubIds.Contains(p.Contract.ClubId));
            var randomPlayers = players.OrderBy(p => RandomNumberHelper.Next()).Take(20);

            var transferListItems = randomPlayers.Select(p =>
            {
                var transferValue = playerHelper.GetTransferValue(p);
                var askingPrice = RandomNumberHelper.Next(transferValue, (int)(transferValue * 1.5));
                return new TransferListItem
                {
                    PlayerId = p.Id,
                    AskingPrice = askingPrice,
                    ClubId = p.Contract.ClubId
                };
            });

            state.TransferListItems.AddRange(transferListItems);
        }
    }

    public void ProcessAITransfers()
    {
        var randomNoOfTransfers = RandomNumberHelper.Next(3);
        for(int i = 0; i < randomNoOfTransfers; i++)
        {
            var randomNumber = RandomNumberHelper.Next(state.TransferListItems.Count);
            var randomPlayer = state.TransferListItems[randomNumber];
            var randomPlayerAskingPrice = randomPlayer.AskingPrice;

            var groupedPlayers = state.Players
                .Where(p => p.Contract != null)
                .GroupBy(p => p.Contract!.ClubId, r => r.Rating)
                .Select(p => new
                {
                    ClubId = p.Key,
                    Rating = p.Average()
                });

            var min = state.Players.First(p => p.Id == randomPlayer.PlayerId).Rating - 5;
            var max = state.Players.First(p => p.Id == randomPlayer.PlayerId).Rating + 5;

            var suitableClub = groupedPlayers
                .FirstOrDefault(p => p.Rating > min && p.Rating < max && p.ClubId != state.MyClub.Id && clubHelper.GetClubById(p.ClubId).TransferBudget > randomPlayer.AskingPrice);
            if (suitableClub == null)
                continue;

            TransferContractedPlayerByPlayerIdAndClubId(randomPlayer.PlayerId, suitableClub.ClubId);

            var previousClub = clubHelper.GetClubById(randomPlayer.ClubId);
            var club = clubHelper.GetClubById(suitableClub.ClubId);
            var player = playerHelper.GetPlayerById(randomPlayer.PlayerId);

            notificationFactory.AddNotification(
                state.Date,
                "Press Officer",
                $"{club.Name} sign {player.Name}",
                $"{player.Name} signs for {club.Name} for {randomPlayer.AskingPriceFriendly} from {previousClub.Name}");
        }
    }
}
