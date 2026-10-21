using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Support;

/// <summary>Builders for small, hand-crafted game worlds used across the tests.</summary>
public static class TestData
{
    // The preferred positions of a balanced 18-man squad that can fill a 4-4-2 and every sub group.
    public static readonly string[] BalancedSquadPositions =
    [
        "GK", "GK",
        "RB", "CB", "CB", "LB", "CB",
        "RM", "CM", "CM", "LM", "CM",
        "ST", "ST", "ST",
        "RW", "LW", "CDM"
    ];

    public static ClubModel Club(int id, string? name = null, int leagueId = 1, int budget = 10_000_000, string formation = "4-4-2") => new()
    {
        Id = id,
        Name = name ?? $"Club {id}",
        LeagueId = leagueId,
        TransferBudget = budget,
        Formation = formation,
        StadiumSize = 10_000
    };

    public static PlayerModel Player(
        int id,
        int? clubId,
        string position = "CM",
        int rating = 60,
        string? clubName = null,
        DateOnly? contractExpiry = null) => new()
    {
        Id = id,
        Name = $"Player {id}",
        Position = position,
        PreferredPosition = position,
        Rating = rating,
        Finishing = 50,
        ShotPower = 50,
        AttackingPosition = 50,
        Contract = clubId == null
            ? null
            : new PlayerModel.ContractModel
            {
                ClubId = clubId.Value,
                ClubName = clubName ?? $"Club {clubId}",
                ExpiryDate = contractExpiry ?? new DateOnly(2028, 6, 30)
            }
    };

    /// <summary>Adds a balanced squad to <paramref name="club"/>, returning the created players.</summary>
    public static List<PlayerModel> AddSquad(IState state, ClubModel club, int firstPlayerId, int baseRating = 60)
    {
        var players = BalancedSquadPositions
            .Select((position, i) => Player(firstPlayerId + i, club.Id, position, baseRating + i % 5, club.Name))
            .ToList();
        state.Players.AddRange(players);
        return players;
    }

    /// <summary>Creates <paramref name="clubCount"/> clubs, each with a balanced squad, in league 1.</summary>
    public static StateModel StateWithClubs(int clubCount, DateOnly? date = null, int myClubId = 1)
    {
        var state = new StateModel { Date = date ?? new DateOnly(2026, 7, 1), MyClubId = myClubId };
        for (var i = 1; i <= clubCount; i++)
        {
            var club = Club(i);
            state.Clubs.Add(club);
            AddSquad(state, club, firstPlayerId: (i - 1) * 100 + 1);
        }
        return state;
    }

    public static LeagueModel League(int id, IEnumerable<ClubModel> clubs, CompetitionType type = CompetitionType.League) => new()
    {
        Id = id,
        Name = $"League {id}",
        Type = type,
        Rank = 1,
        CountryId = 1,
        Year = 2026,
        Clubs = clubs.ToList()
    };

    public static FixtureModel Result(ClubModel home, ClubModel away, int goalsHome, int goalsAway, FixtureType type = FixtureType.Base) => new()
    {
        HomeClub = home,
        AwayClub = away,
        GoalsHome = goalsHome,
        GoalsAway = goalsAway,
        Concluded = true,
        FixtureType = type
    };

    /// <summary>A Cup competition whose draw dates the English league services exclude from their calendars.</summary>
    public static CupModel LeagueCup(params DateOnly[] fixtureDates) => new()
    {
        Id = 99,
        Name = "Cup",
        Type = CompetitionType.Cup,
        DrawSettings = fixtureDates.Select((d, i) => new DrawSettingsModel { Round = i + 1, FixtureDate = d }).ToList()
    };
}
