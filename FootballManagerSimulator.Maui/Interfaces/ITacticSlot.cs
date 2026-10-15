using FootballManagerSimulator.Maui.Enums;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface ITacticSlot
{
    public TacticSlotType TacticSlotType { get; set; }
    public int? PlayerId { get; set; }
}
