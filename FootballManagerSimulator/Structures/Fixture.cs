using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class Fixture : IFixture
{
    public int ID { get; set; }
    public Club HomeTeam { get; set; } = new Club();
    public Club AwayTeam { get; set; } = new Club();
    public int WeekNumber { get; set; }
    public int? GoalsHome { get; set; }
    public int? GoalsAway { get; set; }
    public bool Concluded { get; set; }
    public DateOnly Date { get; set; }
    public ICompetition Competition { get; set; }

    public SerialisableFixtureModel SerialisableFixture()
    {
        return new SerialisableFixtureModel()
        {
            AwayTeamID = AwayTeam.ID,
            ID = ID,
            Concluded = Concluded,
            Date = Date,
            GoalsAway = GoalsAway ?? null,
            GoalsHome = GoalsHome ?? null,
            HomeTeamID = HomeTeam.ID,
            WeekNumber = WeekNumber,
            CompetitionID = Competition.ID
        };
    }

    public class SerialisableFixtureModel
    {
        public int ID { get; set; }
        public int HomeTeamID { get; set; }
        public int AwayTeamID { get; set; }
        public int WeekNumber { get; set; }
        public int? GoalsHome { get; set; }
        public int? GoalsAway { get; set; }
        public bool Concluded { get; set; }
        public DateOnly Date { get; set; }
        public int CompetitionID { get; set; }
    }
}
