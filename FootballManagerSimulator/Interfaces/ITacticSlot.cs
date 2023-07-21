using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ITacticSlot
{
    public TacticSlotType TacticSlotType { get; set; }
    public Player? Player { get; set; }
}
