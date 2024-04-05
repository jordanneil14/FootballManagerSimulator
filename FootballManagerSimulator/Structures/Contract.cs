using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class Contract : IContract
{
    public Club Club { get; set; } = new Club();
    public DateOnly ExpiryDate { get; set; }
    public int WeeklyWage { get; set; }
    public string WeeklyWageFriendly { get => $"£{WeeklyWage:n}"; }
}
