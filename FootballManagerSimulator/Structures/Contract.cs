using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class Contract : IContract
{
    public Team Team { get; set; } = new Team();
    public DateOnly ExpiryDate { get; set; }
    public int WeeklyWage { get; set; }
    public string WeeklyWageFriendly { get => $"£{WeeklyWage:n}"; }
    public DateOnly StartDate { get; set; }

    public SerialisableContractModel SerialisableContract()
    {
        return new SerialisableContractModel
        {
            TeamID = Team.ID,
            ExpiryDate = ExpiryDate,
            StartDate = StartDate,
            WeeklyWage = WeeklyWage
        };
    }

    public class SerialisableContractModel
    {
        public int? TeamID { get; set; }
        public DateOnly ExpiryDate { get; set; }
        public int WeeklyWage { get; set; }
        public DateOnly StartDate { get; set; }
    }
}
