using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ITacticHelper
{
    void PickTacticSlots(Team team);
    void ResetTacticForTeam(Team team);
}
