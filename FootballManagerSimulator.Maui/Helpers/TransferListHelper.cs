using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Helpers;

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
        State.TransferListItems.Add(new TransferListItemModel
        {
            PlayerId = playerId,
            AskingPrice = askingPrice,
            ClubId = State.Players.First(p => p.Id == playerId).Contract!.ClubId
        });
    }

    private static readonly HashSet<int> GkNumbers = [1, 12, 13, 21];

    private int GetAvailableShirtNumber(int clubId, string position)
    {
        var takenNumbers = State.Players
            .Where(p => p.Contract != null && p.Contract.ClubId == clubId && p.ShirtNumber.HasValue)
            .Select(p => p.ShirtNumber!.Value)
            .ToHashSet();

        var isGk = position == "GK";
        var available = Enumerable.Range(1, 99)
            .Where(n => !takenNumbers.Contains(n))
            .Where(n => isGk ? GkNumbers.Contains(n) : !GkNumbers.Contains(n))
            .ToList();

        if (available.Count == 0)
            available = Enumerable.Range(1, 99).Where(n => !takenNumbers.Contains(n)).ToList();

        return available[0];
    }

    private DateOnly GetEndOfCurrentSeasonDate()
    {
        var year = State.Date.Month < 7 ? State.Date.Year : State.Date.Year + 1;
        var date = new DateOnly(year, 6, 30);
        return date;
    }

    public void SignFreeAgentByPlayerId(int playerId, int contractYears)
    {
        var club = State.Clubs.First(p => p.Id == State.MyClubId);

        var expiryDate = GetEndOfCurrentSeasonDate().AddYears(contractYears - 1);

        State.Players.First(p => p.Id == playerId).Contract = new PlayerModel.ContractModel
        {
            ClubId = club.Id,
            ClubName = club.Name,
            ExpiryDate = expiryDate
        };

        var player = State.Players.First(p => p.Id == playerId);
        player.ShirtNumber = GetAvailableShirtNumber(club.Id, player.Position);

        // Add player to reserve tactic slot
        club.TacticSlots
            .First(p => p.TacticSlotType == Enums.TacticSlotType.RES && p.PlayerId == null)
            .PlayerId = playerId;
    }

    public void TransferContractedPlayerByPlayerIdAndClubId(int playerId, int clubId)
    {
        var transferListItem = State.TransferListItems.First(p => p.PlayerId == playerId);

        var existingClubId = State.Players.First(p => p.Id == playerId).Contract?.ClubId ?? transferListItem.ClubId;
        State.Clubs.First(p => p.Id == existingClubId).TransferBudget += transferListItem.AskingPrice;

        var oldClub = State.Clubs.First(p => p.Id == existingClubId);
        var oldSlot = oldClub.TacticSlots.FirstOrDefault(p => p.PlayerId == playerId);
        if (oldSlot != null)
            oldSlot.PlayerId = null;

        State.Players.First(p => p.Id == playerId).Contract!.ClubId = clubId;
        State.Players.First(p => p.Id == playerId).Contract!.ClubName = State.Clubs.First(p => p.Id == clubId).Name;
        State.Players.First(p => p.Id == playerId).ShirtNumber = GetAvailableShirtNumber(clubId, State.Players.First(p => p.Id == playerId).Position);
        State.TransferListItems.Remove(transferListItem);
        State.Clubs.First(p => p.Id == clubId).TransferBudget -= transferListItem.AskingPrice;

        // Add player to reserve tactic slot
        State.Clubs
            .First(p => p.Id == clubId)
            .TacticSlots
            .First(p => p.TacticSlotType == Enums.TacticSlotType.RES && p.PlayerId == null)
            .PlayerId = transferListItem.PlayerId;
    }

    public TransferListItemModel? GetTransferListItemByPlayerId(int playerId)
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
        var myListedItems = State.TransferListItems
            .Where(p => p.ClubId == State.MyClubId)
            .ToList();

        State.TransferListItems.Clear();
        State.TransferListItems.AddRange(myListedItems);

        foreach (var league in State.Competitions.Where(p => p.IsLeague))
        {
            var clubIds = league.Clubs.Select(p => p.Id);
            var players = State.Players.Where(p => p.Contract != null && clubIds.Contains(p.Contract.ClubId) && p.Contract.ClubId != State.MyClubId
                && !IsPlayerTransferListed(p.Id));
            var randomPlayers = players.OrderBy(p => RandomNumberHelper.Next()).Take(20);

            var transferListItems = randomPlayers.Select(p =>
            {
                var transferValue = PlayerHelper.GetTransferValue(p);
                var askingPrice = RandomNumberHelper.Next((int)(transferValue * 0.6), (int)(transferValue * 0.9));
                return new TransferListItemModel
                {
                    PlayerId = p.Id,
                    AskingPrice = askingPrice,
                    ClubId = p.Contract!.ClubId // only contracted players are picked above
                };
            }).ToList();

            State.TransferListItems.AddRange(transferListItems);
        }
    }

    public void ProcessAITransfers()
    {
        if (State.TransferListItems.Count == 0) return;

        ProcessAITransferListBids();
        ProcessUnsolicitedBidsForMyPlayers();
    }

    private void ProcessAITransferListBids()
    {
        var randomNoOfTransfers = RandomNumberHelper.Next(3);
        for (int i = 0; i < randomNoOfTransfers; i++)
        {
            if (State.TransferListItems.Count == 0) return;

            var randomNumber = RandomNumberHelper.Next(State.TransferListItems.Count);
            var randomPlayer = State.TransferListItems[randomNumber];

            // Drop items whose player has since been released or moved clubs.
            var listedPlayer = PlayerHelper.GetPlayerById(randomPlayer.PlayerId);
            if (listedPlayer?.Contract == null || listedPlayer.Contract.ClubId != randomPlayer.ClubId)
            {
                State.TransferListItems.Remove(randomPlayer);
                continue;
            }

            var isMyPlayer = randomPlayer.ClubId == State.MyClubId;

            var suitableClub = FindSuitableClub(randomPlayer.PlayerId, randomPlayer.ClubId, randomPlayer.AskingPrice);
            if (suitableClub == null)
                continue;

            if (isMyPlayer)
            {
                var club = ClubHelper.GetClubById(suitableClub.Value);
                var player = PlayerHelper.GetPlayerById(randomPlayer.PlayerId);
                AddBidNotification(player!, club, randomPlayer.AskingPrice);
                continue;
            }

            TransferContractedPlayerByPlayerIdAndClubId(randomPlayer.PlayerId, suitableClub.Value);

            var previousClub = ClubHelper.GetClubById(randomPlayer.ClubId);
            var buyingClub = ClubHelper.GetClubById(suitableClub.Value);

            NotificationFactory.AddNotification(
                State.Date,
                "Press Officer",
                $"{buyingClub.Name} sign {listedPlayer.Name}",
                $"{listedPlayer.Name} signs for {buyingClub.Name} for {randomPlayer.AskingPriceFriendly} from {previousClub.Name}");
        }
    }

    private void ProcessUnsolicitedBidsForMyPlayers()
    {
        if (RandomNumberHelper.Next(100) >= 8) return;

        var myPlayers = State.Players
            .Where(p => p.Contract != null && p.Contract.ClubId == State.MyClubId
                && !IsPlayerTransferListed(p.Id))
            .ToList();

        if (myPlayers.Count == 0) return;

        var target = myPlayers[RandomNumberHelper.Next(myPlayers.Count)];
        var transferValue = PlayerHelper.GetTransferValue(target);
        var bidAmount = RandomNumberHelper.Next((int)(transferValue * 0.7), (int)(transferValue * 1.3));

        var suitableClubId = FindSuitableClub(target.Id, State.MyClubId!.Value, bidAmount);
        if (suitableClubId == null) return;

        var biddingClub = ClubHelper.GetClubById(suitableClubId.Value);
        AddBidNotification(target, biddingClub, bidAmount);
    }

    private int? FindSuitableClub(int playerId, int currentClubId, int price)
    {
        var player = State.Players.First(p => p.Id == playerId);

        var groupedPlayers = State.Players
            .Where(p => p.Contract != null)
            .GroupBy(p => p.Contract!.ClubId, r => r.Rating)
            .Select(p => new { ClubId = p.Key, Rating = p.Average() });

        var min = player.Rating - 5;
        var max = player.Rating + 5;

        var suitable = groupedPlayers
            .FirstOrDefault(p => p.Rating > min && p.Rating < max
                && p.ClubId != currentClubId
                && p.ClubId != State.MyClubId
                && ClubHelper.GetClubById(p.ClubId).TransferBudget > price);

        return suitable?.ClubId;
    }

    private void AddBidNotification(PlayerModel player, ClubModel biddingClub, int bidAmount)
    {
        State.Notifications.Add(new NotificationModel
        {
            Date = State.Date,
            Recipient = "Director of Football",
            Subject = $"Bid received for {player.Name}",
            Message = $"{biddingClub.Name} have made a bid of £{bidAmount:n0} for {player.Name}. Do you want to accept or reject this offer?",
            RequiresAction = true,
            BidPlayerId = player.Id,
            BidAmount = bidAmount,
            BiddingClubId = biddingClub.Id
        });
    }
}
