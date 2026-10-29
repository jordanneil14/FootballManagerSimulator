using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Helpers;

public class PlayerHelper(IState state) : IPlayerHelper
{
    private readonly IState state = state;

    public ClubModel? GetClubByName(string name)
    {
        return state.Clubs
            .Where(p => p.Name.ToLower() == name.ToLower())
            .FirstOrDefault();
    }

    public bool DoesPlayerPlaysForClub(int playerId, int clubId)
    {
        var player = state.Players.First(p => p.Id == playerId);
        return player.Contract != null && clubId == player.Contract.ClubId;
    }

    public PlayerModel? GetPlayerById(int id)
    {
        return state.Players
            .Where(p => p.Id == id)
            .FirstOrDefault();
    }

    public PlayerModel? GetPlayerByName(string name)
    {
        return state.Players
            .Where(p => p.Name == name)
            .FirstOrDefault();
    }

    public void AddPlayersToState(PlayerData playerData)
    {
        for (var i = 0; i < playerData.Players.Count(); i++)
        {
            var clubName = playerData.Players.ElementAt(i).Contract?.ClubName;
            var club = clubName == null ? null : GetClubByName(clubName);

            playerData.Players.ElementAt(i).Id = i + 1;

            if (club == null)
            {
                playerData.Players.ElementAt(i).Contract = null;
                continue;
            }
            playerData.Players.ElementAt(i).Contract!.ClubId = club.Id;
        }

        state.Players.AddRange(playerData.Players);
    }

    // What a player's rating is worth, adjusted for age (see AgeValueFactor).
    public int GetTransferValue(PlayerModel player)
    {
        if (player.Contract == null) return 0;

        return (int)(RatingValue(player.Rating) * AgeValueFactor(player.GetAge(state.Date)));
    }

    // Ratings don't change, so a young player offers the same ability for many more seasons: clubs pay a
    // premium for youth, peak value runs into the mid-twenties, and it falls away quickly in the thirties
    // as retirement at 35 approaches.
    public static double AgeValueFactor(int age) => age switch
    {
        < 16 => 1.0,  // no birth date on record
        <= 21 => 1.3,
        <= 25 => 1.15,
        <= 28 => 1.0,
        <= 30 => 0.8,
        <= 32 => 0.55,
        33 => 0.35,
        _ => 0.2
    };

    private static int RatingValue(int rating) =>
        rating switch
        {
            < 50 => rating * 500,
            < 53 => rating * 700,
            < 56 => rating * 1000,
            < 59 => rating * 1750,
            < 62 => rating * 2750,
            < 65 => rating * 3500,
            < 68 => rating * 5000,
            < 71 => rating * 8000,
            < 74 => rating * 12000,
            < 77 => rating * 20000,
            < 80 => rating * 50000,
            < 83 => rating * 135000,
            < 86 => rating * 225000,
            < 89 => rating * 350000,
            < 92 => rating * 575000,
            _ => rating * 750000,
        };
}
