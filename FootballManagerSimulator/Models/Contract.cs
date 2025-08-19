using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Models;

public class Contract : IContract
{
    public Club Club { get; set; } = new Club();
    public DateOnly ExpiryDate { get; set; }
}
