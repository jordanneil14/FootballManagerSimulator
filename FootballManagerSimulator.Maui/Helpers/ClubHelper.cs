using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Helpers;

public class ClubHelper(
    IState state) : IClubHelper
{
    private readonly IState State = state;
    // Match simulation looks clubs and players up many times a minute, so avoid scanning the lists each time.
    private readonly IdIndex<ClubModel> ClubsById = new(p => p.Id);
    private readonly IdIndex<PlayerModel> PlayersById = new(p => p.Id);

    public ClubModel? GetClubByName(string name)
    {
        return State.Clubs
            .Where(p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
            .FirstOrDefault();
    }

    public ClubModel GetClubById(int id)
    {
        return ClubsById.Find(State.Clubs, id) ?? throw new InvalidOperationException($"No club with id {id}.");
    }

    public IEnumerable<TacticSlotModel> GetStartingElevenByClub(int clubId)
    {
        return GetClubById(clubId).TacticSlots.Where(p => p.TacticSlotType != Enums.TacticSlotType.SUB && p.TacticSlotType != Enums.TacticSlotType.RES);
    }

    public int GetStartingElevenSumRatingForClub(int clubId)
    {
        var startingEleven = GetClubById(clubId).TacticSlots
            .Where(p => p.TacticSlotType != Enums.TacticSlotType.SUB && p.TacticSlotType != Enums.TacticSlotType.RES);

        // Players picked out of position, or not fully fit, count for less (see PositionFamiliarityRules, FitnessRules).
        var sum = 0.0;
        foreach (var slot in startingEleven)
        {
            if (slot.PlayerId == null) continue;
            var player = PlayersById.Find(State.Players, slot.PlayerId.Value)
                ?? throw new InvalidOperationException($"No player with id {slot.PlayerId}.");
            sum += PositionFamiliarityRules.EffectiveRating(player, slot.TacticSlotType) * FitnessRules.StrengthFactor(player.Fitness);
        }
        return (int)Math.Round(sum);
    }
}
