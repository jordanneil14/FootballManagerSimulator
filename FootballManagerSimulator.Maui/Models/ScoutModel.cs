namespace FootballManagerSimulator.Maui.Models;

public class ScoutModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateOnly HiredDate { get; set; }
    // Reports completed so far; the next one is due this many fortnights (plus one) after hiring.
    public int ReportsMade { get; set; }
    // Wages paid since hiring; the next is due this many months (plus one) after hiring.
    public int MonthsWorked { get; set; }
    public ScoutAssignmentModel? Assignment { get; set; }

    public DateOnly NextReportDate => HiredDate.AddDays(ScoutRules.ReportIntervalDays * (ReportsMade + 1));
    public DateOnly NextPaymentDate => HiredDate.AddMonths(MonthsWorked + 1);
}

public class ScoutAssignmentModel
{
    // Empty means any position.
    public string Position { get; set; } = "";
    // Null means free agents and contracted players alike.
    public bool? FreeAgent { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
    public int? MaxValue { get; set; }
    public bool OnlyWillSign { get; set; } = true;

    public string Describe()
    {
        var parts = new List<string>
        {
            string.IsNullOrEmpty(Position) ? "Any position" : Position,
            FreeAgent switch { true => "Free agents", false => "Contracted", null => "Any contract" }
        };

        if (MinAge != null || MaxAge != null)
            parts.Add($"Age {MinAge?.ToString() ?? "any"}–{MaxAge?.ToString() ?? "any"}");
        if (MinRating != null || MaxRating != null)
            parts.Add($"Overall {MinRating?.ToString() ?? "any"}–{MaxRating?.ToString() ?? "any"}");
        if (MaxValue != null)
            parts.Add($"Value ≤ £{MaxValue:n0}");
        if (OnlyWillSign)
            parts.Add("Will sign");

        return string.Join(" · ", parts);
    }
}
