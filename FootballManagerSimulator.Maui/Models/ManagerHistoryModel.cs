using Newtonsoft.Json;

namespace FootballManagerSimulator.Maui.Models;

/// <summary>
/// The user's career as a manager. Kept on the state (rather than derived from fixtures)
/// because competitions are recreated, and their fixtures discarded, every season.
/// </summary>
public class ManagerHistoryModel
{
    public List<ManagerJobModel> Jobs { get; set; } = [];
    public List<ManagerTrophyModel> Trophies { get; set; } = [];

    // Ignored so loading a save doesn't populate the job it points at a second time.
    [JsonIgnore]
    public ManagerJobModel? CurrentJob =>Jobs.LastOrDefault(p => p.EndDate == null);

    public int Played => Jobs.Sum(p => p.Played);
    public int Won => Jobs.Sum(p => p.Won);
    public int Drawn => Jobs.Sum(p => p.Drawn);
    public int Lost => Jobs.Sum(p => p.Lost);
    public int GoalsFor => Jobs.Sum(p => p.GoalsFor);
    public int GoalsAgainst => Jobs.Sum(p => p.GoalsAgainst);
    public int CleanSheets => Jobs.Sum(p => p.CleanSheets);
    public double WinPercentage => ManagerJobModel.Percentage(Won, Played);
    public double PointsPerGame => ManagerJobModel.PerGame(Won * 3 + Drawn, Played);

    public void StartJob(ClubModel club, DateOnly date)
    {
        if (CurrentJob is { } current)
        {
            if (current.ClubId == club.Id)
                return;
            current.EndDate = date;
        }

        Jobs.Add(new ManagerJobModel { ClubId = club.Id, ClubName = club.Name, StartDate = date });
    }

    public void EndJob(DateOnly date)
    {
        if (CurrentJob is { } current)
            current.EndDate = date;
    }

    public void RecordResult(int goalsFor, int goalsAgainst, bool? won)
    {
        var job = CurrentJob;
        if (job == null)
            return;

        job.Played++;
        if (won == true) job.Won++;
        else if (won == false) job.Lost++;
        else job.Drawn++;

        job.GoalsFor += goalsFor;
        job.GoalsAgainst += goalsAgainst;
        if (goalsAgainst == 0) job.CleanSheets++;
    }

    public void AddTrophy(string name, DateOnly date)
    {
        Trophies.Add(new ManagerTrophyModel
        {
            Name = name,
            ClubName = CurrentJob?.ClubName ?? "",
            Season = SeasonLabel(date),
            Date = date
        });
    }

    // Seasons run July to June, e.g. a cup final in March 2027 belongs to 2026/27.
    public static string SeasonLabel(DateOnly date)
    {
        var startYear = date.Month >= 7 ? date.Year : date.Year - 1;
        return $"{startYear}/{(startYear + 1) % 100:00}";
    }
}

public class ManagerJobModel
{
    public int ClubId { get; set; }
    public string ClubName { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int CleanSheets { get; set; }

    public double WinPercentage => Percentage(Won, Played);
    public double PointsPerGame => PerGame(Won * 3 + Drawn, Played);

    public static double Percentage(int count, int played) => played == 0 ? 0 : count * 100.0 / played;
    public static double PerGame(int total, int played) => played == 0 ? 0 : (double)total / played;
}

public class ManagerTrophyModel
{
    public string Name { get; set; } = "";
    public string ClubName { get; set; } = "";
    public string Season { get; set; } = "";
    public DateOnly Date { get; set; }
}
