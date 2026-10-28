namespace FootballManagerSimulator.Maui.Models;

public class PhysioModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateOnly HiredDate { get; set; }
    public int MonthsWorked { get; set; }
    public DateOnly NextPaymentDate => HiredDate.AddMonths(MonthsWorked + 1);
}

public static class PhysioRules
{
    public const int MaxPhysios = 3;
    public const int MonthlyWage = ScoutRules.MonthlyWage;

    public static double InjuryReduction(int physioCount) => physioCount switch
    {
        >= 3 => 0.30,
        2 => 0.25,
        1 => 0.15,
        _ => 0.0
    };

    public static string InjuryReductionLabel(int physioCount) => physioCount switch
    {
        >= 3 => "30%",
        2 => "25%",
        1 => "15%",
        _ => "0%"
    };

    public static int ReducedDays(int baseDays, int physioCount)
    {
        var reduction = InjuryReduction(physioCount);
        return Math.Max(1, (int)Math.Round(baseDays * (1.0 - reduction)));
    }
}
