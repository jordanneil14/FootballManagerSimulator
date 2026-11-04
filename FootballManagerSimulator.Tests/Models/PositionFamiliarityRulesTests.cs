using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Models;

public class PositionFamiliarityRulesTests
{
    [Theory]
    [InlineData("CB", TacticSlotType.CB)]
    [InlineData("CB/LB", TacticSlotType.CB)]
    [InlineData("CB/LB", TacticSlotType.LB)]
    [InlineData("GK", TacticSlotType.GK)]
    public void EveryListedPosition_IsNatural(string positions, TacticSlotType slot)
    {
        Assert.Equal(PositionFamiliarity.Natural, PositionFamiliarityRules.GetFamiliarity(positions, slot));
    }

    [Theory]
    [InlineData("CB", TacticSlotType.CDM)]
    [InlineData("CDM", TacticSlotType.CB)]
    [InlineData("CM", TacticSlotType.CAM)]
    [InlineData("CAM", TacticSlotType.ST)]
    [InlineData("ST", TacticSlotType.CAM)]
    [InlineData("LB", TacticSlotType.LM)]
    [InlineData("RM", TacticSlotType.RW)]
    [InlineData("ST", TacticSlotType.LW)]
    [InlineData("RW", TacticSlotType.ST)]
    public void NeighbouringRoles_AreAdjacent(string positions, TacticSlotType slot)
    {
        Assert.Equal(PositionFamiliarity.Adjacent, PositionFamiliarityRules.GetFamiliarity(positions, slot));
    }

    [Theory]
    [InlineData("CB", TacticSlotType.RB)]
    [InlineData("LB", TacticSlotType.RB)]
    [InlineData("CAM", TacticSlotType.CDM)]
    [InlineData("LM", TacticSlotType.CM)]
    [InlineData("RB", TacticSlotType.RW)]
    [InlineData("LW", TacticSlotType.RW)]
    [InlineData("RW", TacticSlotType.CAM)]
    public void SameLineOrFlank_IsRelated(string positions, TacticSlotType slot)
    {
        Assert.Equal(PositionFamiliarity.Related, PositionFamiliarityRules.GetFamiliarity(positions, slot));
    }

    [Theory]
    [InlineData("CM", TacticSlotType.RB)]
    [InlineData("ST", TacticSlotType.CB)]
    [InlineData("LB", TacticSlotType.RW)]
    public void FarAwayOutfieldSlots_AreUnfamiliar(string positions, TacticSlotType slot)
    {
        Assert.Equal(PositionFamiliarity.Unfamiliar, PositionFamiliarityRules.GetFamiliarity(positions, slot));
    }

    [Theory]
    [InlineData("ST", TacticSlotType.GK)]
    [InlineData("GK", TacticSlotType.ST)]
    [InlineData("GK", TacticSlotType.CB)]
    public void GoalkeeperAndOutfield_AreASwap(string positions, TacticSlotType slot)
    {
        Assert.Equal(PositionFamiliarity.GoalkeeperSwap, PositionFamiliarityRules.GetFamiliarity(positions, slot));
    }

    [Fact]
    public void BestListedPosition_Wins()
    {
        // CM alone is unfamiliar at RB, but a CM/RM is adjacent there.
        Assert.Equal(PositionFamiliarity.Unfamiliar, PositionFamiliarityRules.GetFamiliarity("CM", TacticSlotType.RB));
        Assert.Equal(PositionFamiliarity.Adjacent, PositionFamiliarityRules.GetFamiliarity("CM/RM", TacticSlotType.RB));
    }

    [Theory]
    [InlineData("LS")]
    [InlineData("LF")]
    public void GeneratorStrikerLabels_CountAsStriker(string position)
    {
        Assert.Equal(PositionFamiliarity.Natural, PositionFamiliarityRules.GetFamiliarity(position, TacticSlotType.ST));
        Assert.Equal(PositionFamiliarity.Adjacent, PositionFamiliarityRules.GetFamiliarity(position, TacticSlotType.CAM));
    }

    [Theory]
    [InlineData("")]
    [InlineData("XYZ")]
    public void UnknownPosition_IsNotPenalised(string position)
    {
        Assert.Equal(PositionFamiliarity.Natural, PositionFamiliarityRules.GetFamiliarity(position, TacticSlotType.GK));
    }

    [Theory]
    [InlineData(TacticSlotType.SUB)]
    [InlineData(TacticSlotType.RES)]
    public void BenchAndReserves_AreNotPenalised(TacticSlotType slot)
    {
        Assert.Equal(PositionFamiliarity.Natural, PositionFamiliarityRules.GetFamiliarity("GK", slot));
    }

    [Theory]
    [InlineData("CB", TacticSlotType.CB, 75)]
    [InlineData("CB", TacticSlotType.CDM, 71)] // 71.25
    [InlineData("CB", TacticSlotType.RB, 64)]  // 63.75
    [InlineData("CM", TacticSlotType.RB, 53)]  // 52.5
    [InlineData("ST", TacticSlotType.GK, 30)]
    public void EffectiveRating_ScalesAndRounds(string position, TacticSlotType slot, int expected)
    {
        var player = TestData.Player(1, null, position, rating: 75);
        Assert.Equal(expected, PositionFamiliarityRules.EffectiveRating(player, slot));
    }
}
