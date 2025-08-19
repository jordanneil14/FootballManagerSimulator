using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Helpers;

public class TransferListHelper(
    IState state,
    IPlayerHelper playerHelper,
    IClubHelper clubHelper,
    INotificationFactory notificationFactory) : ITransferListHelper
{
    private readonly IState State = state;
    private readonly IPlayerHelper PlayerHelper = playerHelper;
    private readonly IClubHelper ClubHelper = clubHelper;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public void AddPlayerToTransferList(int playerId, int askingPrice)
    {
        State.TransferListItems.Add(new TransferListItem
        {
            PlayerId = playerId,
            AskingPrice = askingPrice,
            ClubId = State.Players.First(p => p.Id == playerId).Contract!.ClubId
        });
    }

    private DateOnly GetEndOfCurrentSeasonDate()
    {
        var year = State.Date.Month < 7 ? State.Date.Year : State.Date.Year + 1;
        var date = new DateOnly(year, 6, 30);
        return date;
    }

    public void SignFreeAgentByPlayerId(int playerId)
    {
        State.Players.First(p => p.Id == playerId).Contract = new Player.ContractModel
        {
            ClubId = State.Clubs.First(p => p.Id == State.MyClubId).Id,
            ClubName = State.Clubs.First(p => p.Id == State.MyClubId).Name,
            ExpiryDate = GetEndOfCurrentSeasonDate()
        };

        // Add player to reserve tactic slot
        State.Clubs
            .First(p => p.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id)
            .TacticSlots
            .First(p => p.TacticSlotType == Enums.TacticSlotType.RES && p.PlayerId == null)
            .PlayerId = playerId;
    }

    public void TransferContractedPlayerByPlayerIdAndClubId(int playerId, int clubId)
    {
        var transferListItem = State.TransferListItems.First(p => p.PlayerId == playerId);

        var existingClubId = State.Players.First(p => p.Id == playerId).Contract.ClubId;
        State.Clubs.First(p => p.Id == existingClubId).TransferBudget += transferListItem.AskingPrice;

        State.Players.First(p => p.Id == playerId).Contract!.ClubId = clubId;
        State.TransferListItems.Remove(transferListItem);
        State.Clubs.First(p => p.Id == clubId).TransferBudget -= transferListItem.AskingPrice;

        // Add player to reserve tactic slot
        State.Clubs
            .First(p => p.Id == clubId)
            .TacticSlots
            .First(p => p.TacticSlotType == Enums.TacticSlotType.RES && p.PlayerId == null)
            .PlayerId = transferListItem.PlayerId;
    }

    public TransferListItem? GetTransferListItemByPlayerId(int playerId)
    {
        return State.TransferListItems.FirstOrDefault(p => p.PlayerId == playerId);
    }

    public bool IsPlayerTransferListed(int playerId)
    {
        return State.TransferListItems.Any(p => p.PlayerId == playerId);
    }

    public void RemovePlayerFromTransferList(int playerId)
    {
        State.TransferListItems.RemoveAll(p => p.PlayerId == playerId);
    }

    public void UpdateTransferList()
    {
        State.TransferListItems.Clear();

        foreach (var league in State.Competitions)
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
                    AskingPrice = askingPrice,
                    ClubId = p.Contract.ClubId
                };
            });

            State.TransferListItems.AddRange(transferListItems);
        }
    }

    public void ProcessAITransfers()
    {
        var randomNoOfTransfers = RandomNumberHelper.Next(3);
        for (int i = 0; i < randomNoOfTransfers; i++)
        {
            var randomNumber = RandomNumberHelper.Next(State.TransferListItems.Count);
            var randomPlayer = State.TransferListItems[randomNumber];
            var randomPlayerAskingPrice = randomPlayer.AskingPrice;

            var groupedPlayers = State.Players
                .Where(p => p.Contract != null)
                .GroupBy(p => p.Contract!.ClubId, r => r.Rating)
                .Select(p => new
                {
                    ClubId = p.Key,
                    Rating = p.Average()
                });

            var min = State.Players.First(p => p.Id == randomPlayer.PlayerId).Rating - 5;
            var max = State.Players.First(p => p.Id == randomPlayer.PlayerId).Rating + 5;

            var suitableClub = groupedPlayers
                .FirstOrDefault(p => p.Rating > min && p.Rating < max && p.ClubId != State.Clubs.First(p => p.Id == State.MyClubId).Id && ClubHelper.GetClubById(p.ClubId).TransferBudget > randomPlayer.AskingPrice);
            if (suitableClub == null)
                continue;

            TransferContractedPlayerByPlayerIdAndClubId(randomPlayer.PlayerId, suitableClub.ClubId);

            var previousClub = ClubHelper.GetClubById(randomPlayer.ClubId);
            var club = ClubHelper.GetClubById(suitableClub.ClubId);
            var player = PlayerHelper.GetPlayerById(randomPlayer.PlayerId);

            NotificationFactory.AddNotification(
                State.Date,
                "Press Officer",
                $"{club.Name} sign {player.Name}",
                $"{player.Name} signs for {club.Name} for {randomPlayer.AskingPriceFriendly} from {previousClub.Name}");
        }
    }
}
