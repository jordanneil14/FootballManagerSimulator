using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Models;

public class ContractModel : IContract
{
    public ClubModel Club { get; set; } = new ClubModel();
    public DateOnly ExpiryDate { get; set; }
}
