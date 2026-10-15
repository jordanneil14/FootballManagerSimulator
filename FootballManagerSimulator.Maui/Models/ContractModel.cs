using FootballManagerSimulator.Maui.Interfaces;

namespace FootballManagerSimulator.Maui.Models;

public class ContractModel : IContract
{
    public ClubModel Club { get; set; } = new ClubModel();
    public DateOnly ExpiryDate { get; set; }
}
