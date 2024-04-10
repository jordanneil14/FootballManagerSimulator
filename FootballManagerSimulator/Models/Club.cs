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
    public List<TacticSlot> TacticSlots { get; set; } = new List<TacticSlot>();
}

