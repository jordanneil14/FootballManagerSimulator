using System.Globalization;

namespace FootballManagerSimulator.PlayerGenerator.Tests;

public class PlayerGeneratorTests
{
    private static readonly string[] Goalkeepers = ["GK"];
    private static readonly string[] Defenders = ["CB", "LB", "RB"];
    private static readonly string[] Midfielders = ["CDM", "CM", "CAM", "LM", "RM"];
    private static readonly string[] Forwards = ["ST", "LW", "RW"];

    [Fact]
    public void GenerateYouthIntake_BetterAcademiesProduceBetterPlayers()
    {
        // Same seed, so both intakes roll identical numbers and differ only by the academy.
        var poor = new PlayerGenerator(42).GenerateYouthIntake("Club", 50, "2027-03-01", 1, academyRating: 1);
        var elite = new PlayerGenerator(42).GenerateYouthIntake("Club", 50, "2027-03-01", 1, academyRating: 10);

        // Academy 10 is 9 points above academy 1, worth 2 rating each, unless one of them hit the floor or the ceiling.
        Assert.All(poor.Zip(elite), pair =>
        {
            if (pair.First.Rating > 35 && pair.Second.Rating < PlayerGenerator.MaxYouthRating)
                Assert.Equal(pair.First.Rating + 18, pair.Second.Rating);
            else
                Assert.InRange(pair.Second.Rating, pair.First.Rating, PlayerGenerator.MaxYouthRating);
        });
        Assert.True(elite.Average(p => p.Rating) > poor.Average(p => p.Rating) + 10);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(6)]
    public void GenerateYouthIntake_AnAverageAcademyProducesPlayersAsGoodAsTheSeniorsAtThatLevel(int leagueRank)
    {
        var youth = new PlayerGenerator(1).GenerateYouthIntake("Club", 5000, "2027-03-01", leagueRank, academyRating: 5);
        var generator = new PlayerGenerator(2);
        var seniors = Enumerable.Range(0, 170).SelectMany(_ => generator.GenerateSquad("Club", 30, "2026-07-01", leagueRank)).ToList();

        Assert.InRange(youth.Average(p => p.Rating) - seniors.Average(p => p.Rating), -1.5, 1.5);
    }

    [Fact]
    public void GenerateYouthIntake_PlayersAreSixteenOrSeventeenAndNeverBelowTheFloor()
    {
        var intake = new PlayerGenerator(7).GenerateYouthIntake("Club", 100, "2027-03-01", 5, academyRating: 1);

        Assert.All(intake, p => Assert.InRange(p.Age, 16, 17));
        Assert.All(intake, p => Assert.True(p.Rating >= 35));
    }

    [Fact]
    public void GenerateYouthIntake_GraduatesSignATwoOrThreeYearFirstContract()
    {
        var intake = new PlayerGenerator(8).GenerateYouthIntake("Club", 100, "2027-03-01", 1, academyRating: 5);

        Assert.All(intake, p =>
        {
            Assert.Equal("Club", p.Contract!.ClubName);
            Assert.Equal("2027-03-01", p.Contract.StartDate);
            Assert.Contains(p.Contract.ExpiryDate, new[] { "2029-06-30", "2030-06-30" });
        });
    }

    [Fact]
    public void GenerateSquad_SixthTierSquadsAreWeakerThanFifthTier()
    {
        // Same seed, so both squads roll identical numbers and differ only by the rating band.
        var fifth = new PlayerGenerator(11).GenerateSquad("Club", 30, "2026-07-01", 5);
        var sixth = new PlayerGenerator(11).GenerateSquad("Club", 30, "2026-07-01", 6);

        Assert.True(sixth.Average(p => p.Rating) < fifth.Average(p => p.Rating) - 3);
        Assert.All(sixth, p => Assert.InRange(p.Rating, 30, 65));
    }

    [Theory]
    [InlineData(1, 55, 94)]
    [InlineData(2, 50, 86)]
    [InlineData(3, 45, 81)]
    [InlineData(4, 40, 76)]
    [InlineData(5, 35, 70)]
    [InlineData(6, 30, 65)]
    public void GenerateSquad_RatingsStayWithinTheLeaguesBand(int leagueRank, int min, int max)
    {
        var generator = new PlayerGenerator(12);
        var players = Enumerable.Range(0, 20).SelectMany(_ => generator.GenerateSquad("Club", 30, "2026-07-01", leagueRank));

        Assert.All(players, p => Assert.InRange(p.Rating, min, max));
    }

    [Theory]
    [InlineData(25, 2, 8, 8, 7)]
    [InlineData(30, 2, 10, 10, 8)]
    [InlineData(33, 3, 11, 11, 8)]
    public void GenerateSquad_SplitsTheSquadAcrossTheLines(int size, int gk, int def, int mid, int fwd)
    {
        var squad = new PlayerGenerator(13).GenerateSquad("Club", size, "2026-07-01", 1);

        Assert.Equal(size, squad.Count);
        Assert.Equal(gk, squad.Count(p => Goalkeepers.Contains(p.ClubPosition)));
        Assert.Equal(def, squad.Count(p => Defenders.Contains(p.ClubPosition)));
        Assert.Equal(mid, squad.Count(p => Midfielders.Contains(p.ClubPosition)));
        Assert.Equal(fwd, squad.Count(p => Forwards.Contains(p.ClubPosition)));
    }

    [Fact]
    public void GenerateSquad_GivesEveryPlayerADifferentShirtNumber()
    {
        var squad = new PlayerGenerator(3).GenerateSquad("Club", 33, "2026-07-01", 1);

        Assert.All(squad, p => Assert.InRange(p.ShirtNumber!.Value, 1, 99));
        Assert.Equal(squad.Count, squad.Select(p => p.ShirtNumber).Distinct().Count());
    }

    [Fact]
    public void GenerateSquad_TheBestKeeperWearsOne()
    {
        var squad = new PlayerGenerator(3).GenerateSquad("Club", 25, "2026-07-01", 1);

        var keepers = squad.Where(p => p.ClubPosition == "GK").OrderByDescending(p => p.Rating).ToList();
        Assert.Equal(1, keepers[0].ShirtNumber);
        Assert.Equal(13, keepers[1].ShirtNumber);
    }

    [Fact]
    public void GenerateSquad_ContractsRunOneToFourYearsAndEndOnTheThirtiethOfJune()
    {
        var squad = new PlayerGenerator(14).GenerateSquad("Club", 100, "2026-07-01", 1);

        Assert.All(squad, p =>
        {
            Assert.Equal("Club", p.Contract!.ClubName);
            Assert.Equal("2026-07-01", p.Contract.StartDate);
            Assert.Equal("2026-07-01", p.ClubJoining);
            Assert.Contains(p.Contract.ExpiryDate, new[] { "2027-06-30", "2028-06-30", "2029-06-30", "2030-06-30" });
        });
    }

    [Fact]
    public void Generate_TheSameSeedProducesTheSamePlayers()
    {
        var first = new PlayerGenerator(99).GenerateSquad("Club", 25, "2026-07-01", 2);
        var second = new PlayerGenerator(99).GenerateSquad("Club", 25, "2026-07-01", 2);

        Assert.Equal(first.Select(Describe), second.Select(Describe));
    }

    [Fact]
    public void Generate_AllStatsAreBetweenOneAndNinetyNine()
    {
        var generator = new PlayerGenerator(15);
        var players = Enumerable.Range(0, 10).SelectMany(rank => generator.GenerateSquad("Club", 30, "2026-07-01", rank % 6 + 1))
            .Concat(generator.GeneratePlayers(200, "2026-07-01"))
            .Concat(generator.GenerateYouthIntake("Club", 200, "2027-03-01", 1, academyRating: 10));

        Assert.All(players, p =>
        {
            Assert.All(Stats(p), stat => Assert.InRange(stat, 1, 99));
            Assert.InRange(p.Weakfoot, 1, 5);
            Assert.InRange(p.SkillMoves, 1, 5);
        });
    }

    [Fact]
    public void Generate_KeepersAreBetterInGoalThanOutfieldPlayers()
    {
        var squad = new PlayerGenerator(16).GenerateSquad("Club", 30, "2026-07-01", 3);

        var keepers = squad.Where(p => p.ClubPosition == "GK").ToList();
        var outfield = squad.Where(p => p.ClubPosition != "GK").ToList();
        Assert.All(keepers, p => Assert.True(p.GKReflexes > 30));
        Assert.All(outfield, p => Assert.True(p.GKReflexes <= 15));
    }

    [Fact]
    public void Generate_BirthDateMakesThePlayerTheirAgeOnTheReferenceDate()
    {
        var referenceDate = new DateOnly(2026, 7, 1);
        var players = new PlayerGenerator(17).GeneratePlayers(300, "2026-07-01");

        Assert.All(players, p =>
        {
            var born = DateOnly.ParseExact(p.BirthDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);
            var age = referenceDate.Year - born.Year - (referenceDate < born.AddYears(referenceDate.Year - born.Year) ? 1 : 0);
            Assert.Equal(p.Age, age);
            Assert.InRange(p.Age, 17, 37);
        });
    }

    [Fact]
    public void Generate_PlayersHaveANameAndAMatchingPreferredPosition()
    {
        var players = new PlayerGenerator(18).GeneratePlayers(300, "2026-07-01");

        Assert.All(players, p =>
        {
            Assert.Equal(2, p.Name.Split(' ', 2).Count(part => part.Length > 0));
            Assert.False(string.IsNullOrEmpty(p.Nationality));
            Assert.Equal(p.ClubPosition, p.NationalPosition);
            Assert.StartsWith(p.ClubPosition, p.PreferredPosition);
            Assert.Contains(p.PreferredFoot, new[] { "Left", "Right" });
            Assert.EndsWith(" cm", p.Height);
            Assert.EndsWith(" kg", p.Weight);
        });
    }

    [Fact]
    public void GeneratePlayers_FreeAgentsHaveNoClubOrShirtNumber()
    {
        var players = new PlayerGenerator(19).GeneratePlayers(50, "2026-07-01");

        Assert.Equal(50, players.Count);
        Assert.All(players, p =>
        {
            Assert.Null(p.Contract);
            Assert.Null(p.ShirtNumber);
            Assert.Equal("", p.ClubJoining);
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void GenerateTopUp_GeneratesExactlyTheNumberNeeded(int count)
    {
        var existing = Enumerable.Repeat("CM", 24);

        var players = new PlayerGenerator(5).GenerateTopUp("Club", count, existing, "2026-07-01", 1);

        Assert.Equal(count, players.Count);
    }

    [Fact]
    public void GenerateTopUp_FillsThePositionsTheSquadIsShortOf()
    {
        // A 25-man squad wants 2 keepers and 7 forwards; this one has keepers but has lost two strikers.
        var existing = new[] { "GK", "GK" }
            .Concat(Enumerable.Repeat("CB", 8))
            .Concat(Enumerable.Repeat("CM", 8))
            .Concat(Enumerable.Repeat("ST", 5));

        var players = new PlayerGenerator(5).GenerateTopUp("Club", 2, existing, "2026-07-01", 1);

        Assert.All(players, p => Assert.Contains(p.ClubPosition, Forwards));
    }

    [Fact]
    public void GenerateTopUp_ReplacesAMissingKeeper()
    {
        var existing = new[] { "GK" }
            .Concat(Enumerable.Repeat("CB", 8))
            .Concat(Enumerable.Repeat("CM", 8))
            .Concat(Enumerable.Repeat("ST", 7));

        var players = new PlayerGenerator(5).GenerateTopUp("Club", 1, existing, "2026-07-01", 1);

        Assert.Equal("GK", Assert.Single(players).ClubPosition);
    }

    [Fact]
    public void GenerateTopUp_CoversEveryPositionTheSquadHasNobodyIn()
    {
        // Plenty of players, but no keepers, full backs, wide midfielders or wingers.
        var existing = Enumerable.Repeat("CB", 8)
            .Concat(Enumerable.Repeat("CM", 8))
            .Concat(Enumerable.Repeat("CDM", 2))
            .Concat(Enumerable.Repeat("CAM", 2))
            .Concat(Enumerable.Repeat("ST", 5))
            .ToList();
        var missing = PlayerGenerator.MissingPositions(existing);

        var players = new PlayerGenerator(5).GenerateTopUp("Club", missing.Count, existing, "2026-07-01", 1);

        Assert.Equal(missing.OrderBy(p => p), players.Select(p => p.ClubPosition).OrderBy(p => p));
        Assert.Empty(PlayerGenerator.MissingPositions(existing.Concat(players.Select(p => p.ClubPosition))));
    }

    [Fact]
    public void GenerateTopUp_LeavesShirtNumbersForTheCallerToAssign()
    {
        var players = new PlayerGenerator(5).GenerateTopUp("Club", 5, Enumerable.Repeat("CM", 20), "2026-07-01", 1);

        Assert.All(players, p => Assert.Null(p.ShirtNumber));
    }

    [Fact]
    public void GenerateKeepers_GeneratesOnlyGoalkeepers()
    {
        var keepers = new PlayerGenerator(5).GenerateKeepers("Club", 2, "2026-07-01", 1);

        Assert.Equal(["GK", "GK"], keepers.Select(p => p.ClubPosition));
    }

    [Fact]
    public void MissingPositions_ListsEachPlayerTheSquadIsShortOf()
    {
        var existing = new[] { "GK", "CB", "CB", "CB", "CB", "LB", "LB", "RB", "RB", "CDM", "CM", "CM", "CM", "CM", "CAM", "LM", "RM", "ST", "ST", "LW" };

        Assert.Equal(["GK", "RW"], PlayerGenerator.MissingPositions(existing));
    }

    [Theory]
    [InlineData("CB", 4)]
    [InlineData("LB", 2)]
    [InlineData("RB", 2)]
    [InlineData("CM", 4)]
    [InlineData("ST", 2)]
    [InlineData("GK", 2)]
    public void MissingPositions_RequiresTheUpdatedMinimums(string position, int minimum)
    {
        var existing = Enumerable.Repeat(position, minimum - 1).ToList();
        var missing = PlayerGenerator.MissingPositions(existing);
        Assert.Contains(position, missing);

        existing.Add(position);
        missing = PlayerGenerator.MissingPositions(existing);
        Assert.DoesNotContain(position, missing);
    }

    [Fact]
    public void MissingPositions_ASquadAtEveryMinimumIsShortOfNobody()
    {
        var squad = new[] { "GK", "GK", "CB", "CB", "CB", "CB", "LB", "LB", "RB", "RB", "CDM", "CM", "CM", "CM", "CM", "CAM", "LM", "RM", "ST", "ST", "LW", "RW" };

        Assert.Empty(PlayerGenerator.MissingPositions(squad));
    }

    private static string Describe(GeneratedPlayer p) =>
        $"{p.Name}|{p.Nationality}|{p.ClubPosition}|{p.ShirtNumber}|{p.Rating}|{p.BirthDate}|{string.Join(",", Stats(p))}";

    private static int[] Stats(GeneratedPlayer p) =>
    [
        p.BallControl, p.Dribbling, p.Marking, p.SlidingTackle, p.StandingTackle, p.Aggression, p.Reactions,
        p.AttackingPosition, p.Interceptions, p.Vision, p.Composure, p.Crossing, p.ShortPass, p.LongPass,
        p.Acceleration, p.Speed, p.Stamina, p.Strength, p.Balance, p.Agility, p.Jumping, p.Heading, p.ShotPower,
        p.Finishing, p.LongShots, p.Curve, p.FreekickAccuracy, p.Penalties, p.Volleys,
        p.GKPositioning, p.GKDiving, p.GKKicking, p.GKHandling, p.GKReflexes
    ];
}
