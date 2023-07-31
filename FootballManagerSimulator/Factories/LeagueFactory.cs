using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public class LeagueFactory : ICompetitionFactory
{
    public string CompetitionType => "League";

    private readonly IUtils HelperFunction;

    public LeagueFactory(IUtils helperFunction)
    {
        HelperFunction = helperFunction;
    }

    public ICompetition CreateCompetition(Competition competition)
    {
        var teams = HelperFunction.GetResource<IEnumerable<Team>>("teams.json").Where(p => p.CompetitionID == competition.ID);

        return new League()
        {
            ID = competition.ID,
            Name = competition.Name,
            CompetitionType = CompetitionType,
            Fixtures = GenerateNextRoundOfFixtures(teams.ToList()),
            Teams = teams
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
            Teams = HelperFunction.GetResource<IEnumerable<Team>>("teams.json"),
            Name = serialisableCompetitionModel.Name
        };
    }

    public List<Fixture> GenerateNextRoundOfFixtures(List<Team> teams)
    {
        var output = new List<Fixture>();

        int numRounds = teams.Count - 1;
        int halfSize = teams.Count / 2;

        var teamIndices = new List<Team>(teams);

        teamIndices.RemoveAt(0);

        var teamIdxSize = teamIndices.Count;

        var date = new DateOnly(2022, 07, 02);

        for (int round = 0; round < numRounds; round++)
        {
            int teamIdx = round % teamIdxSize;

            output.Add(new Fixture
            {
                HomeTeam = HelperFunction.GetTeam(teams[0].ID),
                AwayTeam = HelperFunction.GetTeam(teamIndices[teamIdx].ID),
                WeekNumber = round + 1,
                Date = date
            });

            for (int idx = 1; idx < halfSize; idx++)
            {
                int firstTeamIdx = (round + idx) % teamIdxSize;
                int secondTeamIdx = (round + teamIdxSize - idx) % teamIdxSize;

                output.Add(new Fixture
                {
                    HomeTeam = HelperFunction.GetTeam(teamIndices[firstTeamIdx].ID),
                    AwayTeam = HelperFunction.GetTeam(teamIndices[secondTeamIdx].ID),
                    WeekNumber = round + 1,
                    Date = date
                });
            }

            date = date.AddDays(7);
        }

        for (int round = 0; round < numRounds; round++)
        {
            int teamIdx = round % teamIdxSize;

            output.Add(new Fixture
            {
                HomeTeam = HelperFunction.GetTeam(teamIndices[teamIdx].ID),
                AwayTeam = HelperFunction.GetTeam(teams[0].ID),
                WeekNumber = numRounds + round + 1,
                Date = date
            });

            for (int idx = 1; idx < halfSize; idx++)
            {
                int firstTeamIdx = (round + idx) % teamIdxSize;
                int secondTeamIdx = (round + teamIdxSize - idx) % teamIdxSize;

                output.Add(new Fixture
                {
                    HomeTeam = HelperFunction.GetTeam(teamIndices[secondTeamIdx].ID),                
                    AwayTeam = HelperFunction.GetTeam(teamIndices[firstTeamIdx].ID),
                    WeekNumber = numRounds + round + 1,
                    Date = date
                });
            }

            date = date.AddDays(7);
        }

        return output;
    }
}
