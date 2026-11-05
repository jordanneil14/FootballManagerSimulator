using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface IMatchSimulatorHelper
{
    void ProcessMatch(FixtureModel fixture, ICompetition competition);
    void PrepareMatch(FixtureModel fixture);
    void PrepareClub(int clubId);
    void ChooseAIInstructions(FixtureModel fixture);
    void PlanOpponentInstructions(FixtureModel fixture);
    double FitnessAt(FixtureModel fixture, int playerId, int minute);
    List<TacticSlotModel> LineupAt(FixtureModel fixture, bool isHome, int minute);
    void ConcludeFixture(FixtureModel fixture, ICompetition competition);
    void PreGenerateMatch(FixtureModel fixture);
    void GenerateExtraTimeEvents(FixtureModel fixture);
    void GeneratePenaltyEvents(FixtureModel fixture);
    void FinaliseMatch(FixtureModel fixture, ICompetition competition);
    void PopulateFirstLegScores(FixtureModel secondLeg, ICompetition competition);
    void ReGenerateFromMinute(FixtureModel fixture, int fromMinute);
}
