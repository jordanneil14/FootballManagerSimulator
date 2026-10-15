using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface ITacticHelper
{
    void FillEmptyTacticSlotsByClubId(int clubId);
    void ResetTacticForClub(ClubModel club);
}
