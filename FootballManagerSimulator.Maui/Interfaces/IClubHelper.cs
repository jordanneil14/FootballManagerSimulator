using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface IClubHelper
{
    ClubModel? GetClubByName(string name);
    ClubModel GetClubById(int id);
    int GetStartingElevenSumRatingForClub(int clubId);
    IEnumerable<TacticSlotModel> GetStartingElevenByClub(int clubId);
}
