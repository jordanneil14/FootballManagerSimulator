using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class PlayerContract : IPlayerContract
{
    public Team Team { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public int WeeklyWage { get; set; }
    public string WeeklyWageFriendly { get => $"£{WeeklyWage:n}"; }
    public DateOnly StartDate { get; set; }

    public SerialisablePlayerContractModel SerialisablePlayerContract()
    {
        return new SerialisablePlayerContractModel
        {
            TeamID = Team.ID,
            ExpiryDate = ExpiryDate,
            StartDate = StartDate,
            WeeklyWage = WeeklyWage
        };
    }

    public class SerialisablePlayerContractModel
    {
        public int? TeamID { get; set; }
        public DateOnly ExpiryDate { get; set; }
        public int WeeklyWage { get; set; }
        public DateOnly StartDate { get; set; }
    }
}
