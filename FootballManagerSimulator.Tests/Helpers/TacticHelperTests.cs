using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Helpers;

public class TacticHelperTests
{
    private readonly StateModel _state = TestData.StateWithClubs(1);
    private readonly TacticHelper _helper;
    private ClubModel Club => _state.Clubs[0];

    public TacticHelperTests() => _helper = new TacticHelper(_state, new PlayerHelper(_state));

    private static bool IsStarter(TacticSlotModel s) => s.TacticSlotType is not (TacticSlotType.SUB or TacticSlotType.RES);

    public static TheoryData<string> Formations =>
        ["4-4-2", "4-3-3", "4-5-1", "4-1-2-1-2", "4-2-3-1", "4-2-2-2", "4-4-1-1", "3-4-3", "3-4-1-2", "3-5-2", "5-2-1-2", "5-4-1", "5-3-2", "unknown"];

    [Theory]
    [MemberData(nameof(Formations))]
    public void ResetTacticForClub_Creates11Starters7SubsAnd81Reserves(string formation)
    {
        Club.Formation = formation;

        _helper.ResetTacticForClub(Club);

        Assert.Equal(99, Club.TacticSlots.Count);
        Assert.Equal(Enumerable.Range(1, 99), Club.TacticSlots.Select(s => s.Id));
        Assert.Equal(11, Club.TacticSlots.Count(IsStarter));
        Assert.Equal(7, Club.TacticSlots.Count(s => s.TacticSlotType == TacticSlotType.SUB));
        Assert.Equal(81, Club.TacticSlots.Count(s => s.TacticSlotType == TacticSlotType.RES));
        Assert.Single(Club.TacticSlots, s => s.TacticSlotType == TacticSlotType.GK);
        Assert.Equal(TacticSlotType.GK, Club.TacticSlots[0].TacticSlotType);
    }

    [Theory]
    [InlineData("4-3-3", new[] { TacticSlotType.GK, TacticSlotType.RB, TacticSlotType.CB, TacticSlotType.CB, TacticSlotType.LB, TacticSlotType.CM, TacticSlotType.CM, TacticSlotType.CM, TacticSlotType.RW, TacticSlotType.ST, TacticSlotType.LW })]
    [InlineData("4-2-3-1", new[] { TacticSlotType.GK, TacticSlotType.RB, TacticSlotType.CB, TacticSlotType.CB, TacticSlotType.LB, TacticSlotType.CM, TacticSlotType.CM, TacticSlotType.RW, TacticSlotType.CAM, TacticSlotType.LW, TacticSlotType.ST })]
    [InlineData("4-2-2-2", new[] { TacticSlotType.GK, TacticSlotType.RB, TacticSlotType.CB, TacticSlotType.CB, TacticSlotType.LB, TacticSlotType.CDM, TacticSlotType.CDM, TacticSlotType.RW, TacticSlotType.LW, TacticSlotType.ST, TacticSlotType.ST })]
    [InlineData("4-4-1-1", new[] { TacticSlotType.GK, TacticSlotType.RB, TacticSlotType.CB, TacticSlotType.CB, TacticSlotType.LB, TacticSlotType.RM, TacticSlotType.CM, TacticSlotType.CM, TacticSlotType.LM, TacticSlotType.CAM, TacticSlotType.ST })]
    [InlineData("3-5-2", new[] { TacticSlotType.GK, TacticSlotType.CB, TacticSlotType.CB, TacticSlotType.CB, TacticSlotType.RM, TacticSlotType.CM, TacticSlotType.CM, TacticSlotType.CM, TacticSlotType.LM, TacticSlotType.ST, TacticSlotType.ST })]
    [InlineData("5-3-2", new[] { TacticSlotType.GK, TacticSlotType.RB, TacticSlotType.CB, TacticSlotType.CB, TacticSlotType.CB, TacticSlotType.LB, TacticSlotType.CM, TacticSlotType.CM, TacticSlotType.CM, TacticSlotType.ST, TacticSlotType.ST })]
    [InlineData("anything else", new[] { TacticSlotType.GK, TacticSlotType.RB, TacticSlotType.CB, TacticSlotType.CB, TacticSlotType.LB, TacticSlotType.RM, TacticSlotType.CM, TacticSlotType.CM, TacticSlotType.LM, TacticSlotType.ST, TacticSlotType.ST })]
    public void ResetTacticForClub_UsesFormationShape(string formation, TacticSlotType[] expected)
    {
        Club.Formation = formation;
        _helper.ResetTacticForClub(Club);
        Assert.Equal(expected, Club.TacticSlots.Where(IsStarter).Select(s => s.TacticSlotType));
    }

    [Fact]
    public void ResetTacticForClub_PutsEveryContractedPlayerInTheReserves()
    {
        _state.Players.Add(TestData.Player(999, clubId: 2)); // another club's player

        _helper.ResetTacticForClub(Club);

        var clubPlayerIds = _state.Players.Where(p => p.Contract?.ClubId == Club.Id).Select(p => p.Id).OrderBy(i => i);
        var reserveIds = Club.TacticSlots.Where(s => s.TacticSlotType == TacticSlotType.RES && s.PlayerId != null).Select(s => s.PlayerId!.Value).OrderBy(i => i);
        Assert.Equal(clubPlayerIds, reserveIds);
        Assert.All(Club.TacticSlots.Where(s => s.TacticSlotType != TacticSlotType.RES), s => Assert.Null(s.PlayerId));
    }

    [Fact]
    public void FillEmptyTacticSlots_FillsEveryStarterAndSubWithoutDuplicates()
    {
        _helper.ResetTacticForClub(Club);

        _helper.FillEmptyTacticSlotsByClubId(Club.Id);

        Assert.All(Club.TacticSlots.Where(IsStarter), s => Assert.NotNull(s.PlayerId));
        Assert.All(Club.TacticSlots.Where(s => s.TacticSlotType == TacticSlotType.SUB), s => Assert.NotNull(s.PlayerId));
        var assigned = Club.TacticSlots.Where(s => s.PlayerId != null).Select(s => s.PlayerId!.Value).ToList();
        Assert.Equal(assigned.Distinct().Count(), assigned.Count);
        Assert.Equal(TestData.BalancedSquadPositions.Length, assigned.Count); // nobody lost
    }

    [Fact]
    public void FillEmptyTacticSlots_PicksPlayersForTheirPreferredPosition()
    {
        // Wide midfield and wing slots accept each other as fallbacks.
        var accepted = new Dictionary<string, string[]>
        {
            ["RM"] = ["RM", "RW"], ["LM"] = ["LM", "LW"], ["RW"] = ["RW", "RM"], ["LW"] = ["LW", "LM"]
        };
        _helper.ResetTacticForClub(Club);

        _helper.FillEmptyTacticSlotsByClubId(Club.Id);

        foreach (var slot in Club.TacticSlots.Where(IsStarter))
        {
            var slotType = slot.TacticSlotType.ToString();
            var player = _state.Players.First(p => p.Id == slot.PlayerId);
            Assert.Contains(player.PreferredPosition, accepted.GetValueOrDefault(slotType, [slotType]));
        }
    }

    [Fact]
    public void FillEmptyTacticSlots_PicksTheHighestRatedCandidate()
    {
        var goalkeepers = _state.Players.Where(p => p.PreferredPosition == "GK").ToList();
        goalkeepers[0].Rating = 50;
        goalkeepers[1].Rating = 85;
        _helper.ResetTacticForClub(Club);

        _helper.FillEmptyTacticSlotsByClubId(Club.Id);

        Assert.Equal(goalkeepers[1].Id, Club.TacticSlots.First(s => s.TacticSlotType == TacticSlotType.GK).PlayerId);
        // The other keeper is the GK sub.
        Assert.Contains(Club.TacticSlots, s => s.TacticSlotType == TacticSlotType.SUB && s.PlayerId == goalkeepers[0].Id);
    }

    [Fact]
    public void FillEmptyTacticSlots_FallsBackFromWingerToWideMidfielder()
    {
        Club.Formation = "4-3-3";
        // Remove the natural right winger so RW must be filled from a RM.
        _state.Players.RemoveAll(p => p.PreferredPosition == "RW");
        _helper.ResetTacticForClub(Club);

        _helper.FillEmptyTacticSlotsByClubId(Club.Id);

        var rw = Club.TacticSlots.First(s => s.TacticSlotType == TacticSlotType.RW);
        Assert.Equal("RM", _state.Players.First(p => p.Id == rw.PlayerId).PreferredPosition);
    }

    [Fact]
    public void FillEmptyTacticSlots_NeverSelectsSuspendedPlayers()
    {
        var suspended = new[] { "GK", "ST" }.Select(pos => _state.Players.First(p => p.PreferredPosition == pos)).ToList();
        suspended.ForEach(p => p.SuspensionMatches = 1);
        _helper.ResetTacticForClub(Club);

        _helper.FillEmptyTacticSlotsByClubId(Club.Id);

        var selected = Club.TacticSlots.Where(s => s.TacticSlotType != TacticSlotType.RES && s.PlayerId != null).Select(s => s.PlayerId);
        Assert.DoesNotContain(selected, id => suspended.Any(p => p.Id == id));
        // Suspended players stay with the club in the reserves.
        Assert.All(suspended, p => Assert.Contains(Club.TacticSlots, s => s.TacticSlotType == TacticSlotType.RES && s.PlayerId == p.Id));
    }

    [Fact]
    public void FillEmptyTacticSlots_CompactsRemainingReservesIntoTheLowestSlots()
    {
        _helper.ResetTacticForClub(Club);

        _helper.FillEmptyTacticSlotsByClubId(Club.Id);

        var reserves = Club.TacticSlots.Where(s => s.TacticSlotType == TacticSlotType.RES).ToList();
        var filledCount = reserves.Count(s => s.PlayerId != null);
        Assert.All(reserves.Take(filledCount), s => Assert.NotNull(s.PlayerId));
        Assert.All(reserves.Skip(filledCount), s => Assert.Null(s.PlayerId));
    }

    [Fact]
    public void FillEmptyTacticSlots_WithTooFewPlayersLeavesSlotsEmptyRatherThanThrowing()
    {
        _state.Players.RemoveAll(p => p.Id > 5);
        _helper.ResetTacticForClub(Club);

        _helper.FillEmptyTacticSlotsByClubId(Club.Id);

        var outfieldCount = _state.Players.Count(p => p.PreferredPosition != "GK");
        Assert.Equal(5, Club.TacticSlots.Count(s => s.PlayerId != null));
        Assert.Equal(outfieldCount + 1, Club.TacticSlots.Where(IsStarter).Count(s => s.PlayerId != null));
    }

    [Fact]
    public void FillEmptyTacticSlots_NeverPutsGoalkeeperInOutfieldSlot()
    {
        _state.Players.RemoveAll(p => p.PreferredPosition == "ST");
        _helper.ResetTacticForClub(Club);

        _helper.FillEmptyTacticSlotsByClubId(Club.Id);

        var gkIds = _state.Players.Where(p => p.PreferredPosition == "GK").Select(p => p.Id).ToHashSet();
        var outfieldSlots = Club.TacticSlots.Where(s => IsStarter(s) && s.TacticSlotType != TacticSlotType.GK && s.PlayerId != null);
        Assert.All(outfieldSlots, s => Assert.DoesNotContain(s.PlayerId!.Value, gkIds));
    }

    [Fact]
    public void FillEmptyTacticSlots_NeverSelectsInjuredPlayers()
    {
        var injured = new[] { "GK", "ST" }.Select(pos => _state.Players.First(p => p.PreferredPosition == pos)).ToList();
        injured.ForEach(p =>
        {
            p.InjuryEndDate = _state.Date.AddDays(14);
            p.InjuryDescription = "Hamstring injury";
        });
        _helper.ResetTacticForClub(Club);

        _helper.FillEmptyTacticSlotsByClubId(Club.Id);

        var selected = Club.TacticSlots.Where(s => s.TacticSlotType != TacticSlotType.RES && s.PlayerId != null).Select(s => s.PlayerId);
        Assert.DoesNotContain(selected, id => injured.Any(p => p.Id == id));
        Assert.All(injured, p => Assert.Contains(Club.TacticSlots, s => s.TacticSlotType == TacticSlotType.RES && s.PlayerId == p.Id));
    }

    [Fact]
    public void FillEmptyTacticSlots_SelectsPlayersWhoseInjuryHasExpired()
    {
        var recovered = _state.Players.First(p => p.PreferredPosition == "GK");
        recovered.InjuryEndDate = _state.Date.AddDays(-1);
        recovered.InjuryDescription = "Minor knock";
        _helper.ResetTacticForClub(Club);

        _helper.FillEmptyTacticSlotsByClubId(Club.Id);

        var selected = Club.TacticSlots.Where(s => s.TacticSlotType != TacticSlotType.RES && s.PlayerId != null).Select(s => s.PlayerId!.Value);
        Assert.Contains(recovered.Id, selected);
    }
}
