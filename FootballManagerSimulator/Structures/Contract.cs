using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class Contract : IContract
{
    public Club Club { get; set; } = new Club();
    public DateOnly ExpiryDate { get; set; }
    public int WeeklyWage { get; set; }
    public string WeeklyWageFriendly { get => $"£{WeeklyWage:n}"; }
    public DateOnly StartDate { get; set; }

    public SerialisableContractModel? SerialisableContract()
    {
        return new SerialisableContractModel
        {
            ClubID = Club.ID,
            ClubName = Club.Name,
            ExpiryDate = ExpiryDate,
            StartDate = StartDate,
            WeeklyWage = WeeklyWage
        };
    }

    public class SerialisableContractModel
    {
        public int ClubID { get; set; }
        public string ClubName { get; set; } = "";
        public DateOnly ExpiryDate { get; set; }
        public int WeeklyWage { get; set; }
        public DateOnly StartDate { get; set; }
    }
}
