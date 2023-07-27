using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public class LeagueFactory : ICompetitionFactory
{
    public string CompetitionType => "League";

    private readonly IHelperFunction HelperFunction;
    private readonly IFixtureHelper FixtureHelper;

    public LeagueFactory(IHelperFunction helperFunction, IFixtureHelper fixtureHelper)
    {
        HelperFunction = helperFunction;
        FixtureHelper = fixtureHelper;
    }

    public ICompetition CreateLeague(string name, IEnumerable<Team> teams)
    {
        return new League()
        {
            Name = name,
            CompetitionType = CompetitionType,
            Fixtures = FixtureHelper.GenerateFixtures(teams.ToList()),
            Teams = HelperFunction.GetTeams()
        };
    }

    public ICompetition Deserialise(JObject data)
    {
        var serialisableCompetitionModel = data.ToObject<SerialisableCompetitionModel>();

        return new League()
        {
            CompetitionType = CompetitionType,
            Fixtures = serialisableCompetitionModel.Fixtures.Select(p => new Fixture
            {
                AwayTeam = HelperFunction.GetTeam(p.AwayTeamID),
                WeekNumber = p.WeekNumber,
                Concluded = p.Concluded,
                Date = p.Date,
                ID = p.ID,
                GoalsAway = p.GoalsAway,
                GoalsHome = p.GoalsHome,
                HomeTeam = HelperFunction.GetTeam(p.HomeTeamID)
            }).ToList(),
            Teams = HelperFunction.GetTeams(),
            Name = serialisableCompetitionModel.Name
        };
    }
}
