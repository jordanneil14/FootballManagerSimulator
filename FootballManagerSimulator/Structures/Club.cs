using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Structures;

//public interface IClub
//{
//    int Id { get; set; }
//    string Name { get; set; }
//    int TransferBudget { get; set; }
//    int LeagueId { get; set; }
//    string Stadium { get; set; }
//    string TransferBudgetFriendly { get; }
//    string WageBudgetFriendly { get; }
//    int RemainingWageBudget => 0;
//    int WageBudget { get; set; }
//    List<TacticSlot> TacticSlots { get; set; }
//    List<TacticSlot> GenerateBlankTactic();
//}

public class Club
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int TransferBudget { get; set; }
    public int LeagueId { get; set; }
    public string Stadium { get; set; } = "";
    public string TransferBudgetFriendly { get => $"£{TransferBudget:n}"; }
    public string WageBudgetFriendly { get => $"£{RemainingWageBudget:n}"; }
    private int RemainingWageBudget => 0;
    public int WageBudget { get; set; }
    public List<TacticSlot> TacticSlots { get; set; } = GenerateBlankTactic();

    public static List<TacticSlot> GenerateBlankTactic()
    {
        var tacticSlots = new List<TacticSlot>()
        {
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.GK
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.RB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.CB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.CB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.LB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.RM
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.CM
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.LM
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.ST
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.ST
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            },
            new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.SUB
            }
        };

        for (var i = 0; i < 81; i++)
        {
            tacticSlots.Add(new TacticSlot
            {
                PlayerId = null,
                TacticSlotType = TacticSlotType.RES
            });
        }

        return tacticSlots;
    }
}

