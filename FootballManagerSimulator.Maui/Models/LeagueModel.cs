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

	// The club just above the automatic relegation places plays off against the league below to stay up.
	public int RelegationPlayoffPlaces { get; set; }

	public PlayoffFormat PlayoffFormat { get; set; }

	// Once a split league divides, the clubs in its top half. They stay above the rest whatever the points.
	public List<int> TopSplitClubIds { get; set; } = [];

	public bool IsLeague => true;

	// Matches the leagues whose season continues past the fixtures known when it is created:
	// those with a play-off event, and the split league, whose final round is drawn later.
	public bool HasPlayoffs =>
		HasPlayoffEvent
		|| RelegationPlayoffPlaces > 0
		|| Type == CompetitionType.ScottishPremiership;

	// Whether the league's play-off places are drawn into a play-off. The split league's
	// play-off places instead size its top half.
	public bool HasPlayoffEvent =>
		PlayoffPlaces > 0 && Type != CompetitionType.ScottishPremiership;

	// The clubs that go up, assuming there is a league above: the automatic places plus the play-off winner.
	// A relegation play-off's winner only goes up if it is one of ours, not the club from the league above.
	public List<ClubModel> PromotedClubs()
	{
		var promoted = GenerateLeagueTable()
			.Take(AutomaticPromotionPlaces)
			.Select(p => p.Club)
			.ToList();

		var playoffWinner = PlayoffFormat switch
		{
			PlayoffFormat.RelegationPlayoff => RelegationPlayoffSecondLeg()?.ClubWon,
			PlayoffFormat.SerieB => Fixtures.FirstOrDefault(p => IsTwoLeggedPlayoffFinal(p) && p.Concluded)?.ClubWon,
			_ => Fixtures.FirstOrDefault(p => p.FixtureType == FixtureType.PlayoffFinal && p.Concluded)?.ClubWon
		};

		if (playoffWinner != null && Clubs.Any(p => p.Id == playoffWinner.Id))
			promoted.Add(playoffWinner);

		return promoted;
	}

	// The clubs that go down, assuming there is a league below, including a relegation play-off loser.
	public List<ClubModel> RelegatedClubs(LeagueModel? leagueBelow = null)
	{
		var relegated = GenerateLeagueTable()
			.TakeLast(RelegationPlaces)
			.Select(p => p.Club)
			.ToList();

		if (RelegationPlayoffPlaces > 0
			&& leagueBelow is { PlayoffFormat: PlayoffFormat.RelegationPlayoff }
			&& leagueBelow.RelegationPlayoffSecondLeg() is { ClubWon: { } winner } secondLeg)
		{
			var loser = winner.Id == secondLeg.HomeClub.Id ? secondLeg.AwayClub : secondLeg.HomeClub;
			if (Clubs.Any(p => p.Id == loser.Id))
				relegated.Add(loser);
		}

		return relegated;
	}

	// A two-legged final's second leg decides the tie, and it is the only second leg in its round.
	public bool IsTwoLeggedPlayoffFinal(FixtureModel fixture) =>
		PlayoffFormat == PlayoffFormat.SerieB
		&& fixture.FixtureType == FixtureType.PlayoffSecondLeg
		&& Fixtures.Count(p => p.FixtureType == FixtureType.PlayoffSecondLeg && p.Round == fixture.Round) == 1;

	// Where a level play-off tie goes to the higher-placed club instead of extra time: Serie B's
	// second legs, and its preliminaries, the one-off ties with no second leg to follow.
	public bool LevelTieGoesToHigherPlacedClub(FixtureModel fixture) =>
		PlayoffFormat == PlayoffFormat.SerieB
		&& (fixture.FixtureType == FixtureType.PlayoffSecondLeg
			|| (fixture.FixtureType == FixtureType.Playoff && !Fixtures.Any(p =>
				p.FixtureType == FixtureType.PlayoffSecondLeg && p.HomeClub.Id == fixture.AwayClub.Id && p.AwayClub.Id == fixture.HomeClub.Id)));

	// One-off play-off matches that must be settled on the day, through extra time and penalties if need be.
	public bool IsOneOffPlayoffMatch(FixtureModel fixture) =>
		fixture.FixtureType == FixtureType.PlayoffFinal
		|| (fixture.FixtureType == FixtureType.Playoff && PlayoffFormat == PlayoffFormat.NationalLeague);

	public ClubModel HigherPlacedClub(ClubModel a, ClubModel b)
	{
		var table = GenerateLeagueTable().Select(p => p.Club.Id).ToList();
		return table.IndexOf(a.Id) <= table.IndexOf(b.Id) ? a : b;
	}

	private FixtureModel? RelegationPlayoffSecondLeg() =>
		Fixtures.FirstOrDefault(p => p.FixtureType == FixtureType.PlayoffSecondLeg && p.Concluded);

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
            .OrderBy(p => TopSplitClubIds.Count > 0 && !TopSplitClubIds.Contains(p.Club.Id))
            .ThenByDescending(p => p.Points)
            .ThenByDescending(p => p.GoalsScored - p.GoalsConceded)
            .ThenByDescending(p => p.GoalsScored)
            .ThenBy(p => p.Club.Name);
    }
}


