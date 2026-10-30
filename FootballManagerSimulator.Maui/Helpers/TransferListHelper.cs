using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;

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

    // Call once the player is contracted to the club; their previous number is released first.
    private void AssignShirtNumber(PlayerModel player, int clubId)
    {
        player.ShirtNumber = null;
        player.ShirtNumber = ShirtNumberRules.NextAvailable(State.Players, clubId, player.Position);
    }

    private DateOnly GetEndOfCurrentSeasonDate()
    {
        var year = State.Date.Month < 7 ? State.Date.Year : State.Date.Year + 1;
        var date = new DateOnly(year, 6, 30);
        return date;
    }

    public void SignFreeAgentByPlayerId(int playerId, int contractYears)
    {
        var player = State.Players.First(p => p.Id == playerId);
        if (player.IsRetiring(State.Date))
            return;

        SignFreeAgentForClub(player, State.Clubs.First(p => p.Id == State.MyClubId), contractYears);
    }

    private void SignFreeAgentForClub(PlayerModel player, ClubModel club, int contractYears)
    {
        player.Contract = new PlayerModel.ContractModel
        {
            ClubId = club.Id,
            ClubName = club.Name,
            ExpiryDate = GetEndOfCurrentSeasonDate().AddYears(contractYears - 1)
        };

        AssignShirtNumber(player, club.Id);

        // Add player to reserve tactic slot
        club.TacticSlots
            .First(p => p.TacticSlotType == Enums.TacticSlotType.RES && p.PlayerId == null)
            .PlayerId = player.Id;
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
        AssignShirtNumber(State.Players.First(p => p.Id == playerId), clubId);
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
                && !IsPlayerTransferListed(p.Id) && !p.IsRetiring(State.Date));
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

    // Deals between clubs, which only happen while a transfer window is open.
    public void ProcessAITransfers()
    {
        if (State.TransferListItems.Count == 0) return;

        ProcessAITransferListBids();
        ProcessUnsolicitedBidsForMyPlayers();
    }

    private const int MaxAISquadSize = SquadSizeRules.Maximum;
    public const int MaxAIFreeAgentSigningsPerDay = 5;

    // AI clubs pick up out-of-contract players every day, best first, so good players don't sit unsigned.
    // A player only joins a club he'd play for (the same rule as for the user's club: his rating no more
    // than 5 above the club's best-20 average) that wants him (he's within 5 below it). Clubs short of
    // SquadSizeRules.Target get first pick and will take players up to 8 below; otherwise he joins the
    // strongest club that fits, or, if he's too good for all of them, the strongest club with room.
    // Free agents cost no fee.
    public void ProcessAIFreeAgentSignings()
    {
        var freeAgents = State.Players
            .Where(p => p.Contract == null && !p.IsRetiring(State.Date))
            .OrderByDescending(p => p.Rating)
            .ToList();

        if (freeAgents.Count == 0) return;

        var squads = AISquads().Where(p => p.Size < MaxAISquadSize).ToList();

        var signings = 0;
        foreach (var player in freeAgents)
        {
            if (signings == MaxAIFreeAgentSigningsPerDay)
                break;

            var squad = squads.FirstOrDefault(p => p.Size < SquadSizeRules.Target && p.Suits(player, SquadSizeRules.ShortSquadRatingRange))
                ?? squads.FirstOrDefault(p => p.Suits(player, GameEngine.SigningRatingRange))
                ?? StrongestIfTooGoodForAll(squads, player);
            if (squad == null)
                continue;

            SignForAISquad(player, squad);
            if (squad.Size == MaxAISquadSize)
                squads.Remove(squad);
            signings++;
        }
    }

    // On 30 June, before any players are generated: AI clubs left without two keepers or a matchday squad
    // sign the best free agents who'll join them, so the top-up only generates what the pool can't supply.
    public void SignFreeAgentsForShortSquads()
    {
        var freeAgents = State.Players
            .Where(p => p.Contract == null && !p.IsRetiring(State.Date))
            .OrderByDescending(p => p.Rating)
            .ToList();

        foreach (var squad in AISquads())
        {
            while (squad.Keepers < SquadSizeRules.Keepers && Best(p => p.Position == "GK") is { } keeper)
                Sign(keeper);

            while (squad.Size < SquadSizeRules.Minimum && Best(_ => true) is { } player)
                Sign(player);

            PlayerModel? Best(Func<PlayerModel, bool> wanted) =>
                freeAgents.FirstOrDefault(p => wanted(p) && p.Rating - squad.Strength <= GameEngine.SigningRatingRange);

            void Sign(PlayerModel signing)
            {
                freeAgents.Remove(signing);
                SignForAISquad(signing, squad);
            }
        }
    }

    // A player too good for every AI club with room (a 94 when no squad averages 89) doesn't sit out of
    // football: he joins the strongest of them, which is glad to have him. The user's club gets no such
    // exception, so it still needs a squad within 5 of his rating.
    private static AISquad? StrongestIfTooGoodForAll(List<AISquad> squadsStrongestFirst, PlayerModel player) =>
        squadsStrongestFirst.FirstOrDefault() is { } strongest && player.Rating - strongest.Strength > GameEngine.SigningRatingRange
            ? strongest
            : null;

    // Strongest first.
    private List<AISquad> AISquads() =>
        State.Players
            .Where(p => p.Contract != null && p.Contract.ClubId != State.MyClubId)
            .GroupBy(p => p.Contract!.ClubId)
            .Select(p => new AISquad(
                p.Key,
                p.Count(),
                p.Count(r => r.Position == "GK"),
                p.Select(r => r.Rating).OrderByDescending(r => r).Take(GameEngine.SquadAverageBestPlayers).Average()))
            .OrderByDescending(p => p.Strength)
            .ToList();

    private void SignForAISquad(PlayerModel player, AISquad squad)
    {
        squad.Size++;
        if (player.Position == "GK")
            squad.Keepers++;

        var club = ClubHelper.GetClubById(squad.ClubId);
        SignFreeAgentForClub(player, club, RandomNumberHelper.Next(1, 4));

        if (State.ShowTransferNotifications)
            NotificationFactory.AddNotification(
                State.Date,
                "Press Officer",
                $"{club.Name} sign {player.Name}",
                $"{player.Name} has joined {club.Name} on a free transfer.");
    }

    private sealed class AISquad(int clubId, int size, int keepers, double strength)
    {
        public int ClubId { get; } = clubId;
        public int Size { get; set; } = size;
        public int Keepers { get; set; } = keepers;
        public double Strength { get; } = strength;

        // He'd join (no more than 5 above the squad's strength) and the club wants him (less than belowRange under it).
        public bool Suits(PlayerModel player, int belowRange) =>
            player.Rating - Strength <= GameEngine.SigningRatingRange && Strength - player.Rating < belowRange;
    }

    private void ProcessAITransferListBids()
    {
        var randomNoOfTransfers = RandomNumberHelper.Next(8);
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

            var bidChance = TransferListRules.BidChance(randomPlayer.AskingPrice, PlayerHelper.GetTransferValue(listedPlayer));
            if (RandomNumberHelper.NextDouble() >= bidChance)
                continue;

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

            if (State.ShowTransferNotifications)
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

        // No club will move for a player who is about to retire.
        if (player.IsRetiring(State.Date))
            return null;

        var groupedPlayers = State.Players
            .Where(p => p.Contract != null)
            .GroupBy(p => p.Contract!.ClubId, r => r.Rating)
            .Select(p => new { ClubId = p.Key, Size = p.Count(), Rating = p.Average() });

        var min = player.Rating - 5;
        var max = player.Rating + 5;

        // Any suitable club with room in its squad may bid, so the same club doesn't win every deal.
        var suitable = groupedPlayers
            .Where(p => p.Rating > min && p.Rating < max
                && p.Size < MaxAISquadSize
                && p.ClubId != currentClubId
                && p.ClubId != State.MyClubId
                && ClubHelper.GetClubById(p.ClubId).TransferBudget > price)
            .ToList();

        return suitable.Count == 0 ? null : suitable[RandomNumberHelper.Next(suitable.Count)].ClubId;
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
