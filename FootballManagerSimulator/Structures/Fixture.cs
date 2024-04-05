using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class Fixture : IFixture
{
    public int Id { get; set; }
    public Club HomeClub { get; set; } = new Club();
    public Club AwayClub { get; set; } = new Club();
    public int WeekNumber { get; set; }
    public int? GoalsHome { get; set; }
    public int? GoalsAway { get; set; }
    public bool Concluded { get; set; }
    public DateOnly Date { get; set; }
    public int Minute { get; set; }

    public SerialisableFixtureModel SerialisableFixture()
    {
        return new SerialisableFixtureModel()
        {
            AwayClubId = AwayClub.Id,
            Id = Id,
            Concluded = Concluded,
            Date = Date,
            GoalsAway = GoalsAway ?? null,
            GoalsHome = GoalsHome ?? null,
            HomeClubId = HomeClub.Id,
            WeekNumber = WeekNumber
        };
    }

    public class SerialisableFixtureModel
    {
        public int Id { get; set; }
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
