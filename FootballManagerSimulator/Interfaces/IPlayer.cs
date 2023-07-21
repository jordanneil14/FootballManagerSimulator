using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces
{
    public interface IPlayer
    {
        int ID { get; set; }
        string Name { get; set; }
        PlayerContract Contract { get; set; }
    }
}