using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class TacticSlot : ITacticSlot
{
    public int Id { get; set; }
    public TacticSlotType TacticSlotType { get; set; }
    public int? PlayerId { get; set; }
}
