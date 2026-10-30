using System.Globalization;
using FootballManagerSimulator.Maui.Interfaces;

namespace FootballManagerSimulator.Maui.Models;

public class PlayerModel : IPerson
{
    public double ScoringProbability => GetGoalScoringRate();

    public double GetGoalScoringRate()
    {
        return Finishing * ShotPower * (AttackingPosition * 0.75);
    }

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Nationality { get; set; } = "";
    public string NationalKit { get; set; } = "";
    public string Position { get; set; } = "";
    public int? ShirtNumber { get; set; }
    public string ClubJoining { get; set; } = "";
    public DateOnly? ContractExpiry { get; set; }
    public int Rating { get; set; }
    public string Height { get; set; } = "";
    public string Weight { get; set; } = "";
    public string PreferredFoot { get; set; } = "";
    public const int RetirementAge = 35;

    // Stored as MM/dd/yyyy by the player generator
    public string BirthDate { get; set; } = "";

    public int GetAge(DateOnly onDate)
    {
        if (!DateOnly.TryParseExact(BirthDate, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate))
            return 0;

        var age = onDate.Year - birthDate.Year;
        if (onDate < birthDate.AddYears(age)) age--;
        return age;
    }

    // A retiring player will not sign a new contract and leaves the game once their current one expires.
    public bool IsRetiring(DateOnly onDate) => GetAge(onDate) >= RetirementAge;

    // The 30 June the player's last contract ran out. Only meaningful while Contract is null; free agents
    // from the start of the game get the date at the first 30 June they're seen.
    public DateOnly? FreeAgentSince { get; set; }

    // A free agent who goes a whole season without a club gives up and leaves the game.
    public bool HasGivenUp(DateOnly onDate) =>
        Contract == null && FreeAgentSince is { } since && since.AddYears(1) <= onDate;
    public string PreferredPosition { get; set; } = "";
    public string WorkRate { get; set; } = "";
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
    public int SuspensionMatches { get; set; }
    public DateOnly? InjuryEndDate { get; set; }
    public string? InjuryDescription { get; set; }
    public bool IsInjured(DateOnly onDate) => InjuryEndDate != null && InjuryEndDate > onDate;
    // Only players which are signed to a team will have a contract
    public ContractModel? Contract { get; set; }
    public class ContractModel
    {
        public int ClubId { get; set; }
        public string ClubName { get; set; } = "";
        public DateOnly ExpiryDate { get; set; }
    }
}
