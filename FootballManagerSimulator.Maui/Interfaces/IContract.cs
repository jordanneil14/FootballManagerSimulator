using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface IContract
{
    ClubModel Club { get; set; }
    DateOnly ExpiryDate { get; set; }
}
