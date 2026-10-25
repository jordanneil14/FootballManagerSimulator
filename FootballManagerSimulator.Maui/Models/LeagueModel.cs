using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;

namespace FootballManagerSimulator.Maui.Models;

public class LeagueModel : ICompetition
{
    public int Id { get; set; }
    public int Rank { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<FixtureModel> Fixtures { get; set; } = new List<FixtureModel>();
    public List<ClubModel> Clubs { get; set; } = new List<ClubModel>();

    public CompetitionType Type { get; set; } = CompetitionType.League;

    public int Year { get; set; }
    public int CountryId { get; set; }
    public List<DrawSettingsModel> DrawSettings { get; set; } = new List<DrawSettingsModel>();

	public int AutomaticPromotionPlaces { get; set; }
	public int PlayoffPlaces { get; set; }
	public int RelegationPlaces { get; set; }

	public bool IsLeague => true;

	// Matches the leagues that get a play-off event scheduled when the season is created.
	public bool HasPlayoffs =>
		(Type == CompetitionType.EnglishFootballLeague && PlayoffPlaces > 0)
		|| (Type == CompetitionType.EnglishNationalLeague && PlayoffPlaces >= 6);

	// The clubs that go up, assuming there is a league above: the automatic places plus the play-off winner.
	public List<ClubModel> PromotedClubs()
	{
		var promoted = GenerateLeagueTable()
			.Take(AutomaticPromotionPlaces)
			.Select(p => p.Club)
			.ToList();

		var playoffWinner = Fixtures
			.Where(p => p.FixtureType == FixtureType.PlayoffFinal && p.Concluded)
			.Select(p => p.ClubWon)
			.FirstOrDefault();

		if (playoffWinner != null)
			promoted.Add(playoffWinner);

		return promoted;
	}

	// The clubs that go down, assuming there is a league below.
	public List<ClubModel> RelegatedClubs() => GenerateLeagueTable()
		.TakeLast(RelegationPlaces)
		.Select(p => p.Club)
		.ToList();

	public IOrderedEnumerable<LeaguePositionModel> GenerateLeagueTable()
    {
        var table = new List<LeaguePositionModel>();
        foreach (var club in Clubs)
        {
            var clubFixtures = Fixtures.Where(p => p.Concluded && p.FixtureType == FixtureType.Base && (p.HomeClub.Id == club.Id || p.AwayClub.Id == club.Id));
            var homeWinPoints = clubFixtures.Where(p => p.HomeClub.Id == club.Id && p.GoalsHome > p.GoalsAway).Count() * 3;
            var awayWinPoints = clubFixtures.Where(p => p.AwayClub.Id == club.Id && p.GoalsAway > p.GoalsHome).Count() * 3;
            var drawPoints = clubFixtures.Where(p => p.GoalsAway == p.GoalsHome).Count();
            var goalsScoredHome = clubFixtures.Where(p => p.HomeClub.Id == club.Id).Sum(p => p.GoalsHome);
            var goalsScoredAway = clubFixtures.Where(p => p.AwayClub.Id == club.Id).Sum(p => p.GoalsAway);

            var goalsConcededHome = clubFixtures.Where(p => p.HomeClub.Id == club.Id).Sum(p => p.GoalsAway);
            var goalsConcededAway = clubFixtures.Where(p => p.AwayClub.Id == club.Id).Sum(p => p.GoalsHome);

            table.Add(new LeaguePositionModel
            {
                Club = club,
                Points = homeWinPoints + awayWinPoints + drawPoints,
                GoalsScored = goalsScoredHome.GetValueOrDefault() + goalsScoredAway.GetValueOrDefault(),
                GoalsConceded = goalsConcededHome.GetValueOrDefault() + goalsConcededAway.GetValueOrDefault(),
                Played = clubFixtures.Count()
            });
        }

        return table
            .OrderByDescending(p => p.Points)
            .ThenByDescending(p => p.GoalsScored - p.GoalsConceded)
            .ThenByDescending(p => p.GoalsScored)
            .ThenBy(p => p.Club.Name);
    }
}


