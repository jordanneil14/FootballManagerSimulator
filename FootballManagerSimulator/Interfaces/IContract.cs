using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IContract
{
    Club Club { get; set; }
    DateOnly ExpiryDate { get; set; }
}
