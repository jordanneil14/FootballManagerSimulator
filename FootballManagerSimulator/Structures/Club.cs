using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Structures;

public class Club
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public int TransferBudget { get; set; }
    public int CompetitionID { get; set; }

    public string Stadium { get; set; }
    public string TransferBudgetFriendly { get => $"£{TransferBudget:n}"; }

    public string WageBudgetFriendly { get => $"£{RemainingWageBudget:n}"; }

    private int RemainingWageBudget => WageBudget - Players.Sum(p => p.Contract!.WeeklyWage);

    public int WageBudget { get; set; }

    public int FirstTeamTotalRating { get => TacticSlots
            .Where(p => p.TacticSlotType != TacticSlotType.RES && p.TacticSlotType != TacticSlotType.SUB && p.Player != null)
            .Sum(p => p.Player!.Rating); }

    public List<Player> Players { get ; set; } = new List<Player>();

    public List<TacticSlot> TacticSlots { get; set; } = GenerateBlankTactic();

    public override string ToString()
    {
        return Name;
    }

    private static List<TacticSlot> GenerateBlankTactic()
    {
        return new List<TacticSlot>()
        {
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.GK
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.RCB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.RCB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.RCB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.RCB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.ST
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.ST
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                Player = null,
                TacticSlotType = TacticSlotType.SUB
            }
        };
    }

    // Random is static here to ensure the value returned is as random as it can be.
    // Random works off the system clock. That's why when creating new instances of
    // random in quick succession (i.e. in a loop) the values do not appear very random
    private static readonly Random Random = new();

    private static int GenerateWeeklyWage(Player player)
    {
        var rand = Random.Next(player.Rating * 200, player.Rating * 500);
        return rand;
    }

    public void AddPlayer(Player player)
    {
        Players.Add(player);
        player.Contract = new Contract
        {
            Club = this,
            ExpiryDate = player.Contract.ExpiryDate,
            StartDate = player.Contract.StartDate,
            WeeklyWage = GenerateWeeklyWage(player)
        };
        TacticSlots.Add(new TacticSlot
        {
            Player = player,
            TacticSlotType = TacticSlotType.RES
        });
    }

    public SerialisableClubModel SerialisableTeam()
    {
        return new SerialisableClubModel
        {
            ID = ID,
            Name = Name,
            TransferBudget = TransferBudget,
            WageBudget = WageBudget,
            Players = Players.Select(p => p.SerialisablePlayer()),
            TacticSlots = TacticSlots.Select(p => p.SerialisableTacticSlot())
        };
    }

    public class SerialisableClubModel
    {
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public int WageBudget { get; set; }
        public int TransferBudget { get; set; }
        public IEnumerable<Player.SerialisablePlayerModel> Players { get; set; } = new List<Player.SerialisablePlayerModel>();
        public IEnumerable<TacticSlot.SerialisableTacticSlotModel> TacticSlots { get; set; } = new List<TacticSlot.SerialisableTacticSlotModel>();
    }
}

