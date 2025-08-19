using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Helpers;

public class ClubHelper(
    IState state) : IClubHelper
{
    private readonly IState State = state;

    public Club? GetClubByName(string name)
    {
        return State.Clubs
            .Where(p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
            .FirstOrDefault();
    }

    public Club GetClubById(int id)
    {
        return State.Clubs
            .Where(p => p.Id == id)
            .First();
    }

    public IEnumerable<TacticSlot> GetStartingElevenByClub(int clubId)
    {
        return State.Clubs.First(p => p.Id == clubId).TacticSlots.Where(p => p.TacticSlotType != Enums.TacticSlotType.SUB && p.TacticSlotType != Enums.TacticSlotType.RES);
    }

    public int GetStartingElevenSumRatingForClub(int clubId)
    {
        var startingEleven = State.Clubs.First(p => p.Id == clubId).TacticSlots
            .Where(p => p.TacticSlotType != Enums.TacticSlotType.SUB && p.TacticSlotType != Enums.TacticSlotType.RES);

        var sum = 0;
        foreach (var slot in startingEleven)
        {
            if (slot.PlayerId == null) continue;
            var playerRating = State.Players.First(p => p.Id == slot.PlayerId).Rating;
            sum += playerRating;
        }
        return sum;
    }
}
