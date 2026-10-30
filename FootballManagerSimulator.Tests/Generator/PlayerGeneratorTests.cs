using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Generator;

// The generator's own tests live in FootballManagerSimulator.PlayerGenerator.Tests. This one stays here
// because it checks the generator's shirt numbers against the game's ShirtNumberRules, which that project can't see.
public class PlayerGeneratorTests
{
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
}
