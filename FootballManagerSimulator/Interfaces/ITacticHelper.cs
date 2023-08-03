using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ITacticHelper
{
    void PickTacticSlots(Club team);
    void ResetTacticForTeam(Club team);
}
