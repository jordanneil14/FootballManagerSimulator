namespace FootballManagerSimulator.Maui.Models;

public class SettingsModel
{
    public GeneralModel General { get; set; } = new GeneralModel();
    public class GeneralModel
    {
        public string StartDate { get; set; } = "";
        public DateOnly StartDateAsDate => DateOnly.Parse(StartDate);
    }
    public List<ClubModel> Clubs { get; set; } = new List<ClubModel>();
    public IEnumerable<CompetitionModel> Competitions { get; set; } = new List<CompetitionModel>();
    public IEnumerable<IdNameModel> Countries { get; set; } = new List<IdNameModel>();

    // "England - Division 1", so notifications say which country's league they mean.
    public string LeagueHeading(LeagueModel league) =>
        Countries.FirstOrDefault(p => p.Id == league.CountryId) is { } country ? $"{country.Name} - {league.Name}" : league.Name;

    public const string OtherCountry = "Other";

    // Competitions with no country, such as friendlies, come back as "Other".
    public string CountryName(int competitionId)
    {
        var countryId = Competitions.FirstOrDefault(p => p.Id == competitionId)?.CountryId ?? 0;
        return Countries.FirstOrDefault(p => p.Id == countryId)?.Name ?? OtherCountry;
    }

    public class IdNameModel
    {
        public string Name { get; set; } = string.Empty;
        public int Id { get; set; }
    }

}
