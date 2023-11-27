using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class Player : IPerson
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Nationality { get; set; }
    public string NationalKit { get; set; }
    public string Position { get; set; }
    public int? ShirtNumber { get; set; }
    public string ClubJoining { get; set; }
    public DateOnly? ContractExpiry { get; set; }
    public int Rating { get; set; }
    public string Height { get; set; }
    public string Weight { get; set; }
    public string PreferredFoot { get; set; }
    public string BirthDate { get; set; }
    public int Age { get; set; }
    public string PreferredPosition { get; set; }
    public string WorkRate { get; set; }
    public int Weakfoot { get; set; }
    public int SkillMoves { get; set; }
    public int BallControl { get; set; }
    public int Dribbling { get; set; }
    public int Marking { get; set; }
    public int SlidingTackle { get; set; }
    public int StandingTackle { get; set; }
    public int Aggression { get; set; }
    public int Reactions { get; set; }
    public int AttackingPosition { get; set; }
    public int Interceptions { get; set; }
    public int Vision { get; set; }
    public int Composure { get; set; }
    public int Crossing { get; set; }
    public int ShortPass { get; set; }
    public int LongPass { get; set; }
    public int Acceleration { get; set; }
    public int Speed { get; set; }
    public int Stamina { get; set; }
    public int Strength { get; set; }
    public int Balance { get; set; }
    public int Agility { get; set; }
    public int Jumping { get; set; }
    public int Heading { get; set; }
    public int ShotPower { get; set; }
    public int Finishing { get; set; }
    public int LongShots { get; set; }
    public int Curve { get; set; }
    public int FreekickAccuracy { get; set; }
    public int Penalties { get; set; }
    public int Volleys { get; set; }
    public int GKPositioning { get; set; }
    public int GKDiving { get; set; }
    public int GKKicking { get; set; }
    public int GKHandling { get; set; }
    public int GKReflexes { get; set; }
    // Only players which are signed to a team will have a contract
    public Contract? Contract { get; set; }

    public SerialisablePlayerModel SerialisablePlayer()
    {
        return new SerialisablePlayerModel
        {
            Contract = Contract?.SerialisableContract(),
            Age = Age,
            ID = ID, 
            Name = Name,
            Rating = Rating,
            ShirtNumber = ShirtNumber,
            Acceleration = Acceleration,
            Aggression = Aggression,
            Agility = Agility,
            AttackingPosition = AttackingPosition,
            Balance = Balance,
            BallControl = BallControl,
            BirthDate = BirthDate,
            ClubJoining = ClubJoining,
            Composure = Composure,
            Crossing = Crossing,
            Curve = Curve,
            Dribbling = Dribbling,
            Finishing = Finishing,
            FreekickAccuracy = FreekickAccuracy,
            GKDiving = GKDiving,
            GKHandling = GKHandling,
            GKKicking = GKKicking,
            GKPositioning = GKPositioning,
            GKReflexes = GKReflexes,
            Heading = Heading,
            Height = Height,
            Interceptions = Interceptions,
            Jumping = Jumping,
            LongPass = LongPass,
            LongShots = LongShots,
            Marking = Marking,
            Nationality = Nationality,
            NationalKit = NationalKit,
            Penalties = Penalties,
            Position = Position,
            PreferredFoot = PreferredFoot,
            PreferredPosition = PreferredPosition,
            Reactions = Reactions,
            ShortPass = ShortPass,
            ShotPower = ShotPower,
            SkillMoves = SkillMoves,
            SlidingTackle = SlidingTackle,
            Speed = Speed,
            Stamina = Stamina,
            StandingTackle = StandingTackle,
            Strength = Strength,
            Vision = Vision,
            Volleys = Volleys,
            Weakfoot = Weakfoot,
            Weight = Weight,
            WorkRate = WorkRate
        };
    }

    public static Player FromPlayerData(SerialisablePlayerModel item, int id, Club? club)
    {
        var player = new Player
        {
            Age = item.Age,
            ID = id,
            Name = item.Name,
            Position = item.PreferredPosition,
            Crossing = item.Crossing,
            Curve = item.Curve,
            Dribbling = item.Dribbling,
            Finishing = item.Finishing,
            GKDiving = item.GKDiving,
            FreekickAccuracy = item.FreekickAccuracy,
            GKPositioning = item.GKPositioning,
            GKKicking = item.GKKicking,
            Heading = item.Heading,
            GKHandling = item.GKHandling,
            Acceleration = item.Acceleration,
            Aggression = item.Aggression,
            Agility = item.Agility,
            AttackingPosition = item.AttackingPosition,
            Balance = item.Balance,
            BallControl = item.BallControl,
            BirthDate = item.BirthDate,
            Composure = item.Composure,
            Height = item.Height,
            Jumping = item.Jumping,
            GKReflexes = item.GKReflexes,
            LongShots = item.LongShots,
            LongPass = item.LongPass,
            Interceptions = item.Interceptions,
            Marking = item.Marking,
            Penalties = item.Penalties,
            WorkRate = item.WorkRate,
            PreferredFoot = item.PreferredFoot,
            Reactions = item.Reactions,
            ShortPass = item.ShortPass,
            ShotPower = item.ShotPower,
            SlidingTackle = item.SlidingTackle,
            Speed = item.Speed,
            SkillMoves = item.SkillMoves,
            Stamina = item.Stamina,
            StandingTackle = item.StandingTackle,
            Strength = item.Strength,
            Nationality = item.Nationality,
            Volleys = item.Volleys,
            Weakfoot = item.Weakfoot,
            Weight = item.Weight,
            PreferredPosition = item.PreferredPosition,
            Vision = item.Vision,
            Rating = item.Rating,
            ShirtNumber = item.ShirtNumber,
            Contract = club == null ? null : new Contract
            {
                Club = club,
                ExpiryDate = item.Contract.ExpiryDate,
                StartDate = item.Contract.StartDate,
                WeeklyWage = item.Contract.WeeklyWage
            }
        };
        club?.AddPlayer(player);
        return player;
    }

    public class SerialisablePlayerModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Nationality { get; set; }
        public string NationalKit { get; set; }
        public string Position { get; set; }
        public int? ShirtNumber { get; set; }
        public string ClubJoining { get; set; }
        public int Rating { get; set; }
        public string Height { get; set; }
        public string Weight { get; set; }
        public string PreferredFoot { get; set; }
        public string BirthDate { get; set; }
        public int Age { get; set; }
        public string PreferredPosition { get; set; }
        public string WorkRate { get; set; }
        public int Weakfoot { get; set; }
        public int SkillMoves { get; set; }
        public int BallControl { get; set; }
        public int Dribbling { get; set; }
        public int Marking { get; set; }
        public int SlidingTackle { get; set; }
        public int StandingTackle { get; set; }
        public int Aggression { get; set; }
        public int Reactions { get; set; }
        public int AttackingPosition { get; set; }
        public int Interceptions { get; set; }
        public int Vision { get; set; }
        public int Composure { get; set; }
        public int Crossing { get; set; }
        public int ShortPass { get; set; }
        public int LongPass { get; set; }
        public int Acceleration { get; set; }
        public int Speed { get; set; }
        public int Stamina { get; set; }
        public int Strength { get; set; }
        public int Balance { get; set; }
        public int Agility { get; set; }
        public int Jumping { get; set; }
        public int Heading { get; set; }
        public int ShotPower { get; set; }
        public int Finishing { get; set; }
        public int LongShots { get; set; }
        public int Curve { get; set; }
        public int FreekickAccuracy { get; set; }
        public int Penalties { get; set; }
        public int Volleys { get; set; }
        public int GKPositioning { get; set; }
        public int GKDiving { get; set; }
        public int GKKicking { get; set; }
        public int GKHandling { get; set; }
        public int GKReflexes { get; set; }
        public Contract.SerialisableContractModel? Contract { get; set; }
    }
}
