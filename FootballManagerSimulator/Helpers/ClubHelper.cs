using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Helpers;

public class ClubHelper(
    IState state) : IClubHelper
{
    public Club? GetClubByName(string name)
    {
        return state.Clubs
            .Where(p => p.Name.ToLower() == name.ToLower())
            .FirstOrDefault();
    }

    public Club GetClubById(int id)
    {
        return state.Clubs
            .Where(p => p.Id == id)
            .First();
    }

    public IEnumerable<TacticSlot> GetStartingElevenByClub(int clubId)
    {
        return state.Clubs.First(p => p.Id == clubId).TacticSlots.Where(p => p.TacticSlotType != Enums.TacticSlotType.SUB && p.TacticSlotType != Enums.TacticSlotType.RES);
    }

    public int GetStartingElevenSumRatingForClub(int clubId)
    {
        var startingEleven = state.Clubs.First(p => p.Id == clubId).TacticSlots
            .Where(p => p.TacticSlotType != Enums.TacticSlotType.SUB && p.TacticSlotType != Enums.TacticSlotType.RES);

        var sum = 0;
        foreach (var slot in startingEleven)
        {
            if (slot.PlayerId == null) continue;
            var playerRating = state.Players.First(p => p.Id == slot.PlayerId).Rating;
            sum += playerRating;
        }
        return sum;
    }
}
