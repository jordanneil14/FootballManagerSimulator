using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;

namespace FootballManagerSimulator.Maui.Models;

public class FixtureModel : IFixture
{
    public int Id { get; set; }
    public ClubModel HomeClub { get; set; } = new ClubModel();
    public ClubModel AwayClub { get; set; } = new ClubModel();
    public int Round { get; set; }
    public int? GoalsHome { get; set; }
    public int? GoalsAway { get; set; }
    public bool Concluded { get; set; }
    public DateOnly Date { get; set; }
    public int Minute { get; set; }
    public ClubModel? ClubWon { get; set; } = null;

    public List<GoalModel> HomeScorers { get; set; } = new List<GoalModel>();
    public List<GoalModel> AwayScorers { get; set; } = new List<GoalModel>();
    public List<CardModel> HomeCards { get; set; } = new List<CardModel>();
    public List<CardModel> AwayCards { get; set; } = new List<CardModel>();
    public TimeOnly KickOffTime { get; set; }
    public FixtureType FixtureType { get; set; } = FixtureType.Base;
    public int? FirstLegGoalsHome { get; set; }
    public int? FirstLegGoalsAway { get; set; }
    public List<InjuryModel> HomeInjuries { get; set; } = new List<InjuryModel>();
    public List<InjuryModel> AwayInjuries { get; set; } = new List<InjuryModel>();
    // The approach an AI side settled on the day before (see PlanOpponentInstructions), so the
    // analyst's report matches the match. Null means the side chooses on match day.
    public TeamInstructionsModel? HomePlannedInstructions { get; set; }
    public TeamInstructionsModel? AwayPlannedInstructions { get; set; }
    // When each side changed its pressing during the match, so fatigue counts the minutes actually pressed.
    public List<PressingSpellModel> HomePressing { get; set; } = new List<PressingSpellModel>();
    public List<PressingSpellModel> AwayPressing { get; set; } = new List<PressingSpellModel>();
    // Penalties awarded to each side, whatever the outcome.
    public List<PenaltyModel> HomePenalties { get; set; } = new List<PenaltyModel>();
    public List<PenaltyModel> AwayPenalties { get; set; } = new List<PenaltyModel>();
    // Substitutions made during the match, so fitness counts the minutes each player was on the pitch.
    public List<SubstitutionModel> HomeSubstitutions { get; set; } = new List<SubstitutionModel>();
    public List<SubstitutionModel> AwaySubstitutions { get; set; } = new List<SubstitutionModel>();
    // An AI side's XI and bench at kick-off, kept while the match is played so the side at any minute is
    // these slots with the substitutions so far applied, and its substitutes come from this bench. The
    // user's side changes its own slots live, so isn't kept. Cleared at full time.
    public List<TacticSlotModel> HomeStartingSlots { get; set; } = new List<TacticSlotModel>();
    public List<TacticSlotModel> AwayStartingSlots { get; set; } = new List<TacticSlotModel>();
    // Set at full time once the players' fitness has been charged for this match.
    public bool FitnessApplied { get; set; }
    // Set once an English Cup tie's prize money has gone to both clubs, so it is never paid twice.
    public bool PrizeMoneyPaid { get; set; }
    // Everyone who took the pitch: the starting XI at kick-off, plus each substitute as they come on.
    public List<int> HomePlayerIds { get; set; } = new List<int>();
    public List<int> AwayPlayerIds { get; set; } = new List<int>();
}
