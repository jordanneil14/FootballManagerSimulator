using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IContract
{
    Club? Club { get; set; }
    DateOnly ExpiryDate { get; set; }
    int WeeklyWage { get; set; }
    DateOnly StartDate { get; set; }
}
