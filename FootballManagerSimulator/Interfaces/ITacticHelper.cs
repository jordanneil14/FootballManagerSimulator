using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ITacticHelper
{
    void FillEmptyTacticSlotsByClubId(int clubId);
    void ResetTacticForClub(Club club);
}
