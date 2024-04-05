using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Helpers;

public class ClubHelper : IClubHelper
{
    private readonly IState State;

    public ClubHelper(
        IState state)
    {
        State = state;
    }

    public Club? GetClubByName(string name)
    {
        return State.Clubs
            .Where(p => p.Name.ToLower() == name.ToLower())
            .FirstOrDefault();
    }

    public Club GetClubById(int id)
    {
        return State.Clubs
            .Where(p => p.Id == id)
            .First();
    }

    public int GetStartingElevenSumRatingForClub(Club club)
    {
        var startingEleven = club.TacticSlots
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
