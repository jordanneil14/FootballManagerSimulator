using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Services;

/// <summary>Where each starter stands on the pitch views (Tactics and the live match).</summary>
public static class PitchLayout
{
    // Maps a starter to a position on the pitch (percentages), grouping starters into
    // horizontal lines by role and spreading each line evenly across the width.
    public static (double x, double y) Position(List<TacticSlotModel> starters, int index)
    {
        var line = LineOf(starters[index].TacticSlotType);
        var sameLine = starters.Select((s, i) => (s, i)).Where(t => LineOf(t.s.TacticSlotType) == line)
            .OrderBy(t => SideOrder(t.s.TacticSlotType)).ToList();
        var posInLine = sameLine.FindIndex(t => t.i == index);
        var count = sameLine.Count;
        var hasWide = sameLine.Any(t => SideOrder(t.s.TacticSlotType) != 1);
        var spread = count >= 4 || (hasWide && count >= 2) ? 64.0 : count == 3 ? 46.0 : 28.0;
        var x = count == 1 ? 50.0 : (50.0 - spread / 2) + posInLine * (spread / (count - 1));
        var y = line switch { 0 => 90.0, 1 => 72.0, 2 => 60.0, 3 => 46.0, 4 => 30.0, _ => 14.0 };
        return (x, y);
    }

    private static int SideOrder(TacticSlotType type) => type switch
    {
        TacticSlotType.LB or TacticSlotType.LM or TacticSlotType.LW or TacticSlotType.LF or TacticSlotType.LS => 0,
        TacticSlotType.RB or TacticSlotType.RM or TacticSlotType.RW => 2,
        _ => 1
    };

    private static int LineOf(TacticSlotType type) => type switch
    {
        TacticSlotType.GK => 0,
        TacticSlotType.RB or TacticSlotType.LB or TacticSlotType.CB => 1,
        TacticSlotType.CDM => 2,
        TacticSlotType.RM or TacticSlotType.LM or TacticSlotType.CM => 3,
        TacticSlotType.CAM or TacticSlotType.LW or TacticSlotType.RW => 4,
        _ => 5
    };
}
