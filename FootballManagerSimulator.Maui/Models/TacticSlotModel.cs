using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;

namespace FootballManagerSimulator.Maui.Models;

public class TacticSlotModel : ITacticSlot
{
    public int Id { get; set; }
    public TacticSlotType TacticSlotType { get; set; }
    public int? PlayerId { get; set; }
}
