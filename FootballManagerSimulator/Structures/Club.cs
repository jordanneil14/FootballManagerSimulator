using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Structures;

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

    private static List<TacticSlot> GenerateBlankTactic()
    {
        return new List<TacticSlot>()
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
    }
}

