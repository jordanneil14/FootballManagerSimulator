using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class Fixture : IFixture
{
    public int ID { get; set; }
    public Team HomeTeam { get; set; } = new Team();
    public Team AwayTeam { get; set; } = new Team();
    public int WeekNumber { get; set; }
    public int? GoalsHome { get; set; }
    public int? GoalsAway { get; set; }
    public bool Concluded { get; set; }
    public DateOnly Date { get; set; }

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
            WeekNumber = WeekNumber
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
    }
}
