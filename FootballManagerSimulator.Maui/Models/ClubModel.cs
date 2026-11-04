namespace FootballManagerSimulator.Maui.Models;

public class ClubModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int TransferBudget { get; set; }
    public int LeagueId { get; set; }
    public string Stadium { get; set; } = "";
    public int StadiumSize { get; set; }
    // 1-10; drives the quality of the 1 March youth intake
    public int AcademyRating { get; set; }
	public string TransferBudgetFriendly { get => $"£{TransferBudget:n0}"; }
	public int WageBudget { get; set; }
    public List<TacticSlotModel> TacticSlots { get; set; } = new List<TacticSlotModel>();
    public string Formation { get; set; } = "4-4-2";
    // Mentality, pressing etc. Old saves without it get the neutral defaults.
    public TeamInstructionsModel Instructions { get; set; } = new TeamInstructionsModel();
    // In order of preference: the first of them on the pitch takes the penalty.
    public List<int> PenaltyTakerIds { get; set; } = new List<int>();
    public string Colour { get; set; } = "";
    public double JobSecurity { get; set; } = BoardConfidenceRules.Starting;
    public DateOnly? VacantUntil { get; set; }
}

