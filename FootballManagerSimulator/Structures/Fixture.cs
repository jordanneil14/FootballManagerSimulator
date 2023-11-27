using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class Fixture : IFixture
{
    public int ID { get; set; }
    public Club HomeClub { get; set; } = new Club();
    public Club AwayClub { get; set; } = new Club();
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
            AwayClubId = AwayClub.ID,
            ID = ID,
            Concluded = Concluded,
            Date = Date,
            GoalsAway = GoalsAway ?? null,
            GoalsHome = GoalsHome ?? null,
            HomeClubId = HomeClub.ID,
            WeekNumber = WeekNumber,
            CompetitionID = Competition.ID
        };
    }

    public class SerialisableFixtureModel
    {
        public int ID { get; set; }
        public int HomeClubId { get; set; }
        public int AwayClubId { get; set; }
        public int WeekNumber { get; set; }
        public int? GoalsHome { get; set; }
        public int? GoalsAway { get; set; }
        public bool Concluded { get; set; }
        public DateOnly Date { get; set; }
        public int CompetitionID { get; set; }
    }
}
