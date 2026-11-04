namespace FootballManagerSimulator.Maui.Models;

// Watches the next opponent and sends a pre-match report the day before each competitive match.
public class AnalystModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateOnly HiredDate { get; set; }
    // Wages paid since hiring; the next is due this many months (plus one) after hiring.
    public int MonthsWorked { get; set; }
    public DateOnly NextPaymentDate => HiredDate.AddMonths(MonthsWorked + 1);
}

public static class AnalystRules
{
    public const int MaxAnalysts = 1;
    public const int MonthlyWage = ScoutRules.MonthlyWage;
}
