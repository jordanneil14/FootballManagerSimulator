using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.PlayerGenerator;

namespace FootballManagerSimulator.Tests.Generator;

public class PlayerGeneratorTests
{
    [Fact]
    public void GenerateYouthIntake_BetterAcademiesProduceBetterPlayers()
    {
        // Same seed, so both intakes roll identical numbers and differ only by the academy.
        var poor = new PlayerGenerator.PlayerGenerator(42).GenerateYouthIntake("Club", 50, "2027-03-01", 1, academyRating: 1);
        var elite = new PlayerGenerator.PlayerGenerator(42).GenerateYouthIntake("Club", 50, "2027-03-01", 1, academyRating: 10);

        // Academy 10 is 9 points above academy 1, worth 2 rating each, unless the poor player hit the floor of 35.
        // An elite graduate who reached 90+ may also have been knocked down 5.
        Assert.All(poor.Zip(elite), pair =>
        {
            if (pair.First.Rating > 35 && pair.First.Rating + 18 >= 90)
                Assert.Contains(pair.Second.Rating, new[] { pair.First.Rating + 18, pair.First.Rating + 13 });
            else if (pair.First.Rating > 35)
                Assert.Equal(pair.First.Rating + 18, pair.Second.Rating);
            else
                Assert.InRange(pair.Second.Rating, 35, 53);
        });
        Assert.True(elite.Average(p => p.Rating) > poor.Average(p => p.Rating) + 10);
    }

    [Fact]
    public void GenerateSquad_SixthTierSquadsAreWeakerThanFifthTier()
    {
        // Same seed, so both squads roll identical numbers and differ only by the rating band.
        var fifth = new PlayerGenerator.PlayerGenerator(11).GenerateSquad("Club", 30, "2026-07-01", 5);
        var sixth = new PlayerGenerator.PlayerGenerator(11).GenerateSquad("Club", 30, "2026-07-01", 6);

        Assert.True(sixth.Average(p => p.Rating) < fifth.Average(p => p.Rating) - 3);
        Assert.All(sixth, p => Assert.InRange(p.Rating, 30, 65));
    }

    [Fact]
    public void GenerateSquad_GivesEachLinesBestPlayersItsReservedNumbersAndTheRestUnreservedOnes()
    {
        var squad = new PlayerGenerator.PlayerGenerator(3).GenerateSquad("Club", 28, "2026-07-01", 1);
        Assert.Equal(squad.Count, squad.Select(p => p.ShirtNumber).Distinct().Count());

        foreach (var line in squad.GroupBy(p => ShirtNumberRules.ReservedNumbersFor(p.ClubPosition)))
        {
            var best = line.OrderByDescending(p => p.Rating).Take(line.Key.Length).Select(p => p.ShirtNumber!.Value);
            Assert.Equal(line.Key.Take(line.Count()), best);
        }

        var reserved = ShirtNumberRules.GoalkeeperNumbers.Concat(ShirtNumberRules.DefenderNumbers)
            .Concat(ShirtNumberRules.MidfielderNumbers).Concat(ShirtNumberRules.AttackerNumbers).ToHashSet();
        var others = squad.Where(p => !reserved.Contains(p.ShirtNumber!.Value)).OrderByDescending(p => p.Rating).ToList();
        var expected = Enumerable.Range(1, 99).Where(n => !reserved.Contains(n)).Take(others.Count).Select(n => (int?)n);
        Assert.Equal(expected, others.Select(p => p.ShirtNumber));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void GenerateTopUp_GeneratesExactlyTheNumberNeeded(int count)
    {
        var existing = Enumerable.Repeat("CM", 24);

        var players = new PlayerGenerator.PlayerGenerator(5).GenerateTopUp("Club", count, existing, "2026-07-01", 1);

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

        var players = new PlayerGenerator.PlayerGenerator(5).GenerateTopUp("Club", 2, existing, "2026-07-01", 1);

        Assert.All(players, p => Assert.Contains(p.ClubPosition, new[] { "ST", "LW", "RW" }));
    }

    [Fact]
    public void GenerateTopUp_ReplacesAMissingKeeper()
    {
        var existing = new[] { "GK" }
            .Concat(Enumerable.Repeat("CB", 8))
            .Concat(Enumerable.Repeat("CM", 8))
            .Concat(Enumerable.Repeat("ST", 7));

        var players = new PlayerGenerator.PlayerGenerator(5).GenerateTopUp("Club", 1, existing, "2026-07-01", 1);

        Assert.Equal("GK", Assert.Single(players).ClubPosition);
    }

    [Fact]
    public void GenerateYouthIntake_PlayersAreSixteenOrSeventeenAndNeverBelowTheFloor()
    {
        var intake = new PlayerGenerator.PlayerGenerator(7).GenerateYouthIntake("Club", 100, "2027-03-01", 5, academyRating: 1);

        Assert.All(intake, p => Assert.InRange(p.Age, 16, 17));
        Assert.All(intake, p => Assert.True(p.Rating >= 35));
    }
}
