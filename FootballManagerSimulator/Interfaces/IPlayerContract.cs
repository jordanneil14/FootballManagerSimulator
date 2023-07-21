using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IPlayerContract
{
    Team? Team { get; set; }
    DateOnly ExpiryDate { get; set; }
    int WeeklyWage { get; set; }
    DateOnly StartDate { get; set; }

}
