using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Interfaces;

public interface ITacticSlot
{
    public TacticSlotType TacticSlotType { get; set; }
    public int? PlayerId { get; set; }
}
