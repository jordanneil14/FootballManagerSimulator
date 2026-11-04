namespace FootballManagerSimulator.Maui.Models;

// The neutral option of each is 0, so a club or save with no instructions plays as before.
public enum Mentality
{
    Balanced = 0,
    Defensive = 1,
    Attacking = 2
}

public enum Pressing
{
    Medium = 0,
    Low = 1,
    High = 2
}

// How a club sets up to play, on top of its formation and line-up (see TacticalModifierRules).
public class TeamInstructionsModel
{
    public Mentality Mentality { get; set; } = Mentality.Balanced;
    public Pressing Pressing { get; set; } = Pressing.Medium;

    public TeamInstructionsModel Clone() => new() { Mentality = Mentality, Pressing = Pressing };
}

// One stretch of a match during which a side kept the same pressing, from FromMinute until the next spell.
public class PressingSpellModel
{
    public int FromMinute { get; set; }
    public Pressing Pressing { get; set; }
}

public static class PressingLog
{
    // Starts a match's log with the side's pressing at kick-off.
    public static void Start(List<PressingSpellModel> log, Pressing pressing)
    {
        log.Clear();
        log.Add(new PressingSpellModel { FromMinute = 0, Pressing = pressing });
    }

    // A change at fromMinute: later spells are dropped (that part of the match is being replayed),
    // and a new spell starts if the pressing has changed.
    public static void ChangeAt(List<PressingSpellModel> log, int fromMinute, Pressing pressing)
    {
        // A spell starting at fromMinute is replaced (several changes in the same pause).
        log.RemoveAll(s => s.FromMinute >= fromMinute);
        if (log.Count > 0 && log[^1].Pressing == pressing)
            return;
        log.Add(new PressingSpellModel { FromMinute = fromMinute, Pressing = pressing });
    }

    // Minutes spent pressing high before the given minute. With no log (a match from an older
    // save), the side is taken to have kept its current pressing since kick-off.
    public static int MinutesPressedHigh(IReadOnlyList<PressingSpellModel> log, int minute, Pressing current)
    {
        if (log.Count == 0)
            return current == Pressing.High ? minute : 0;

        var total = 0;
        for (var i = 0; i < log.Count; i++)
        {
            if (log[i].Pressing != Pressing.High) continue;
            var from = log[i].FromMinute;
            var to = i + 1 < log.Count ? log[i + 1].FromMinute : int.MaxValue;
            total += Math.Max(0, Math.Min(to, minute) - from);
        }
        return total;
    }
}
