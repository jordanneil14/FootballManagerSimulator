using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class TacticSlot : ITacticSlot
{
    public TacticSlotType TacticSlotType { get; set; }
    public int? PlayerId { get; set; }
    
}
