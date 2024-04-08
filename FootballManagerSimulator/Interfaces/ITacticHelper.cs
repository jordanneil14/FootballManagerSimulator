using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ITacticHelper
{
    void FillEmptyTacticSlotsByClub(Club club);
    void ResetTacticForClub(Club club);
    //void ResetTacticForClub(Club club);
}
