using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Interfaces;

public interface IContract
{
    ClubModel Club { get; set; }
    DateOnly ExpiryDate { get; set; }
}
