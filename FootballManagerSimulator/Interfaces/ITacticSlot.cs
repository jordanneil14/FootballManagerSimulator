using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ITacticSlot
{
    public TacticSlotType TacticSlotType { get; set; }
    public int? PlayerID { get; set; }
}
