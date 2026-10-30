using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Tests.Helpers;

/// <summary>
/// The real day-by-day processing over the real DI container, but on a small hand-built world
/// (no settings competitions) so single-day rules can be checked without playing a season.
/// </summary>
public class ProcessHelperTests : IDisposable
{
    private const string Aged37 = "01/15/1990";

    private readonly GameHost _host = new(new SettingsModel());
    private IState State => _host.State;

    public ProcessHelperTests()
    {
        State.MyClubId = 1;
        for (var i = 1; i <= 3; i++)
        {
            var club = TestData.Club(i);
            club.AcademyRating = 5;
            State.Clubs.Add(club);
            TestData.AddSquad(State, club, firstPlayerId: (i - 1) * 100 + 1);
        }

        var tactics = _host.Get<ITacticHelper>();
        State.Clubs.ForEach(tactics.ResetTacticForClub);
        State.ManagerHistory.StartJob(State.Clubs[0], new DateOnly(2026, 7, 1));
    }

    public void Dispose() => _host.Dispose();

    private void AdvanceTo(DateOnly date)
    {
        State.Date = date.AddDays(-1);
        _host.Get<IProcessHelper>().Process();
    }

    private PlayerModel PlayerAt(int clubId, int index = 0) =>
        State.Players.Where(p => p.Contract?.ClubId == clubId).ElementAt(index);

    // ----- Expiring contracts ------------------------------------------------------------

    [Theory]
    [InlineData(2026, 12, 30, "6 months")]
    [InlineData(2027, 3, 30, "3 months")]
    public void ExpiringContracts_ListsMyPlayersWhoseContractsEndThisSeason(int y, int m, int d, string when)
    {
        var expiring = PlayerAt(1, 0);
        var retiring = PlayerAt(1, 1);
        var staying = PlayerAt(1, 2);
        var otherClubs = PlayerAt(2, 0);
        foreach (var p in new[] { expiring, retiring, otherClubs })
            p.Contract!.ExpiryDate = new DateOnly(2027, 6, 30);
        retiring.BirthDate = Aged37;

        AdvanceTo(new DateOnly(y, m, d));

        var notification = Assert.Single(State.Notifications, n => n.Subject == "Expiring Contracts");
        Assert.Contains($"expire in {when}, on 30 June 2027", notification.Message);
        Assert.Contains($"{expiring.Name} (", notification.Message);
        Assert.Contains($"{retiring.Name} ({retiring.PreferredPosition}, {retiring.Rating}) - retiring", notification.Message);
        Assert.DoesNotContain(staying.Name + " (", notification.Message);
        Assert.DoesNotContain(otherClubs.Name + " (", notification.Message);
    }

    [Theory]
    [InlineData(2026, 12, 29)]
    [InlineData(2027, 3, 31)]
    public void ExpiringContracts_SaysNothingOnOtherDays(int y, int m, int d)
    {
        PlayerAt(1).Contract!.ExpiryDate = new DateOnly(2027, 6, 30);

        AdvanceTo(new DateOnly(y, m, d));

        Assert.DoesNotContain(State.Notifications, n => n.Subject == "Expiring Contracts");
    }

    [Fact]
    public void ExpiringContracts_SaysNothingWhenNoneOfMyContractsAreEnding()
    {
        AdvanceTo(new DateOnly(2026, 12, 30));

        Assert.DoesNotContain(State.Notifications, n => n.Subject == "Expiring Contracts");
    }

    // ----- AI contract renewals ----------------------------------------------------------

    [Fact]
    public void AIRenewals_ReSignSomeAIPlayersOn30MarchButNeverTouchMine()
    {
        // Ageing fringe players at each AI club, so some contracts are all but certain to be let go.
        foreach (var club in State.Clubs.Skip(1))
            for (var i = 0; i < 12; i++)
            {
                var fringe = TestData.Player(9300 + 100 * club.Id + i, club.Id, rating: 50, clubName: club.Name);
                fringe.BirthDate = "01/15/1994";
                State.Players.Add(fringe);
            }

        var seasonEnd = new DateOnly(2027, 6, 30);
        foreach (var player in State.Players)
            player.Contract!.ExpiryDate = seasonEnd;
        var retiring = PlayerAt(2, 0);
        retiring.BirthDate = Aged37;

        AdvanceTo(new DateOnly(2027, 3, 30));

        var mine = State.Players.Where(p => p.Contract!.ClubId == 1);
        var ai = State.Players.Where(p => p.Contract!.ClubId != 1).ToList();
        Assert.All(mine, p => Assert.Equal(seasonEnd, p.Contract!.ExpiryDate));
        Assert.Equal(seasonEnd, retiring.Contract!.ExpiryDate);
        Assert.All(ai, p => Assert.Contains(p.Contract!.ExpiryDate, Enumerable.Range(0, 5).Select(y => seasonEnd.AddYears(y))));
        Assert.Contains(ai, p => p.Contract!.ExpiryDate > seasonEnd);
        Assert.Contains(ai, p => p.Contract!.ExpiryDate == seasonEnd && p != retiring);
    }

    [Fact]
    public void AIRenewals_KeepTheFirstTeamAndLetSurplusPlayersGo()
    {
        // Club 2 gets a 30-man squad of 26-year-olds with distinct ratings, best first.
        var club = State.Clubs[1];
        for (var id = 9200; State.Players.Count(p => p.Contract?.ClubId == club.Id) < 30; id++)
            State.Players.Add(TestData.Player(id, club.Id, clubName: club.Name));
        var squad = State.Players.Where(p => p.Contract?.ClubId == club.Id).ToList();
        for (var i = 0; i < squad.Count; i++)
        {
            squad[i].Rating = 90 - i;
            squad[i].BirthDate = "01/15/2001";
        }

        var seasonEnd = new DateOnly(2027, 6, 30);
        int firstTeamKept = 0, surplusKept = 0;
        const int trials = 40;
        for (var trial = 0; trial < trials; trial++)
        {
            squad.ForEach(p => p.Contract!.ExpiryDate = seasonEnd);
            AdvanceTo(new DateOnly(2027, 3, 30));
            firstTeamKept += squad.Take(11).Count(p => p.Contract!.ExpiryDate > seasonEnd);
            surplusKept += squad.Skip(25).Count(p => p.Contract!.ExpiryDate > seasonEnd);
        }

        // Expected 98% of the first team and 50% of ranks 26-30.
        Assert.InRange(firstTeamKept / (11.0 * trials), 0.92, 1);
        Assert.InRange(surplusKept / (5.0 * trials), 0.3, 0.7);
    }

    [Fact]
    public void AIRenewals_LeaveContractsThatRunBeyondThisSeasonAlone()
    {
        var later = new DateOnly(2029, 6, 30);
        foreach (var player in State.Players)
            player.Contract!.ExpiryDate = later;

        AdvanceTo(new DateOnly(2027, 3, 30));

        Assert.All(State.Players, p => Assert.Equal(later, p.Contract!.ExpiryDate));
    }

    // ----- Transfer window ---------------------------------------------------------------

    [Theory]
    [InlineData(2027, 6, 15, "Transfer Window Open", "closes on 31 August 2027")]
    [InlineData(2027, 9, 1, "Transfer Window Closed", "reopens on 1 January 2028")]
    [InlineData(2028, 1, 1, "Transfer Window Open", "closes on 31 January 2028")]
    [InlineData(2028, 2, 1, "Transfer Window Closed", "reopens on 15 June 2028")]
    public void TransferWindow_AnnouncesTheDayItOpensOrCloses(int y, int m, int d, string subject, string message)
    {
        AdvanceTo(new DateOnly(y, m, d));

        var notification = Assert.Single(State.Notifications, n => n.Subject.StartsWith("Transfer Window"));
        Assert.Equal(subject, notification.Subject);
        Assert.Contains(message, notification.Message);
    }

    [Theory]
    [InlineData(2027, 6, 16)]
    [InlineData(2027, 9, 2)]
    [InlineData(2028, 1, 15)]
    public void TransferWindow_SaysNothingOnOtherDays(int y, int m, int d)
    {
        AdvanceTo(new DateOnly(y, m, d));

        Assert.DoesNotContain(State.Notifications, n => n.Subject.StartsWith("Transfer Window"));
    }

    // ----- End of season retirements ------------------------------------------------------

    [Fact]
    public void EndOfSeason_RetiresOutOfContractPlayersAged35PlusAndTellsMeAboutMine()
    {
        var seasonEnd = new DateOnly(2027, 6, 30);

        var myRetiree = PlayerAt(1, 0);
        myRetiree.BirthDate = Aged37;
        myRetiree.Contract!.ExpiryDate = seasonEnd;
        _host.Get<ITransferListHelper>().AddPlayerToTransferList(myRetiree.Id, 1_000);

        var aiRetiree = PlayerAt(2, 0);
        aiRetiree.BirthDate = Aged37;
        aiRetiree.Contract!.ExpiryDate = seasonEnd;

        var released = PlayerAt(1, 1); // aged 0 (no birth date), so released rather than retired
        released.Contract!.ExpiryDate = seasonEnd;

        var stillContracted = PlayerAt(1, 2); // 35+ but with a year left on their deal
        stillContracted.BirthDate = Aged37;

        var retiringFreeAgent = TestData.Player(9000, clubId: null);
        retiringFreeAgent.BirthDate = Aged37;
        State.Players.Add(retiringFreeAgent);

        AdvanceTo(seasonEnd);

        Assert.DoesNotContain(State.Players, p => p.Id == myRetiree.Id || p.Id == aiRetiree.Id || p.Id == retiringFreeAgent.Id);
        Assert.DoesNotContain(State.TransferListItems, p => p.PlayerId == myRetiree.Id);
        Assert.DoesNotContain(State.Clubs.SelectMany(c => c.TacticSlots), s => s.PlayerId == myRetiree.Id || s.PlayerId == aiRetiree.Id);

        // Released, not retired (club 2, a player down after its retirement, may well sign him straight away).
        Assert.Contains(released, State.Players);
        Assert.NotEqual(1, released.Contract?.ClubId);
        Assert.Equal(1, stillContracted.Contract!.ClubId);

        var notification = Assert.Single(State.Notifications, n => n.Subject == "Retirements");
        Assert.Contains(myRetiree.Name, notification.Message);
        Assert.DoesNotContain(aiRetiree.Name, notification.Message);
    }

    [Fact]
    public void EndOfSeason_FreeAgentsWhoGoAWholeSeasonUnsignedLeaveTheGame()
    {
        // Rated 40, so no club wants them.
        var unsignedAllSeason = TestData.Player(9500, null, rating: 40);
        unsignedAllSeason.FreeAgentSince = new DateOnly(2026, 6, 30);
        var neverDated = TestData.Player(9501, null, rating: 40); // from the start of the game or an older save
        State.Players.AddRange([unsignedAllSeason, neverDated]);
        // Squads keep their size, so no AI club is short and signs them on 30 June.
        foreach (var player in State.Players.Where(p => p.Contract != null))
            player.Contract!.ExpiryDate = new DateOnly(2030, 6, 30);
        var released = PlayerAt(1, 0); // mine, so no AI club is a player down
        released.Rating = 40;
        released.Contract!.ExpiryDate = new DateOnly(2027, 6, 30);

        AdvanceTo(new DateOnly(2027, 6, 30));

        Assert.DoesNotContain(unsignedAllSeason, State.Players);
        Assert.Contains(neverDated, State.Players);
        Assert.Equal(new DateOnly(2027, 6, 30), neverDated.FreeAgentSince);
        Assert.Contains(released, State.Players);
        Assert.Null(released.Contract);
        Assert.Equal(new DateOnly(2027, 6, 30), released.FreeAgentSince);

        // A season later, both are gone.
        AdvanceTo(new DateOnly(2028, 6, 30));
        Assert.DoesNotContain(neverDated, State.Players);
        Assert.DoesNotContain(released, State.Players);
    }

    [Fact]
    public void EndOfSeason_NoRetirementNotificationWhenNoneOfMyPlayersRetire()
    {
        var aiRetiree = PlayerAt(2, 0);
        aiRetiree.BirthDate = Aged37;
        aiRetiree.Contract!.ExpiryDate = new DateOnly(2027, 6, 30);

        AdvanceTo(new DateOnly(2027, 6, 30));

        Assert.DoesNotContain(State.Notifications, n => n.Subject == "Retirements");
    }

    [Fact]
    public void EndOfSeason_GivesASquadOfTheMinimumOrMoreOnlyTheKeepersItIsShortOf()
    {
        // 28 players with no keepers, and short of full backs, centre backs and centre mids.
        var club = State.Clubs[1];
        foreach (var keeper in State.Players.Where(p => p.Contract?.ClubId == club.Id && p.Position == "GK"))
            keeper.Position = keeper.PreferredPosition = "ST";
        for (var id = 9100; id < 9110; id++)
            State.Players.Add(TestData.Player(id, club.Id, "CAM", clubName: club.Name));
        var before = State.Players.Count(p => p.Contract?.ClubId == club.Id);
        Assert.True(before >= 25);
        Assert.Contains("LB", PlayerGenerator.PlayerGenerator.MissingPositions(State.Players.Where(p => p.Contract?.ClubId == club.Id).Select(p => p.Position)));

        AdvanceTo(new DateOnly(2027, 6, 30));

        var squad = State.Players.Where(p => p.Contract?.ClubId == club.Id).Select(p => p.Position).ToList();
        Assert.Equal(before + 2, squad.Count);
        Assert.Equal(2, squad.Count(p => p == "GK"));
        Assert.Equal(["CB", "LB", "RB", "CM"], PlayerGenerator.PlayerGenerator.MissingPositions(squad));
    }

    [Fact]
    public void EndOfSeason_SignsFreeAgentsBeforeGeneratingPlayersForASquadBelowTheMinimum()
    {
        // Club 2 is down to 14; the pool has two players for it, so only the rest are generated.
        var club = State.Clubs[1];
        var squad = State.Players.Where(p => p.Contract?.ClubId == club.Id && p.Position != "GK").Take(4).ToList();
        State.Players.RemoveAll(squad.Contains);
        var freeAgents = new[] { TestData.Player(9400, null, rating: 62), TestData.Player(9401, null, rating: 61) };
        State.Players.AddRange(freeAgents);
        var maxId = State.Players.Max(p => p.Id);

        AdvanceTo(new DateOnly(2027, 6, 30));

        var after = State.Players.Where(p => p.Contract?.ClubId == club.Id).ToList();
        Assert.All(freeAgents, p => Assert.Equal(club.Id, p.Contract?.ClubId));
        Assert.True(after.Count >= SquadSizeRules.Minimum);
        Assert.InRange(after.Count(p => p.Id > maxId), 2, SquadSizeRules.Minimum);
        Assert.Empty(PlayerGenerator.PlayerGenerator.MissingPositions(after.Select(p => p.Position)));
    }

    [Fact]
    public void FreeAgentsAreSignedOutsideTheTransferWindow()
    {
        var freeAgent = TestData.Player(9400, null, rating: 62);
        State.Players.Add(freeAgent);

        AdvanceTo(new DateOnly(2026, 10, 15));

        Assert.False(TransferWindowRules.IsOpen(State.Date));
        Assert.NotNull(freeAgent.Contract);
        Assert.NotEqual(1, freeAgent.Contract.ClubId);
    }

    [Fact]
    public void EndOfSeason_KeepsAnAcademyUpgradeAlreadyPaidForSoItStillCompletes()
    {
        var completion = new DateOnly(2027, 8, 15);
        State.Events.Add(ActivatorUtilities.CreateInstance<AcademyUpgradeEvent>(_host.Services, completion, false));

        AdvanceTo(new DateOnly(2027, 6, 30));
        Assert.Single(State.Events.OfType<AcademyUpgradeEvent>(), e => !e.IsCompleted);

        AdvanceTo(completion);
        Assert.Equal(6, State.Clubs[0].AcademyRating);
    }

    // ----- Youth intake -------------------------------------------------------------------

    [Fact]
    public void YouthIntake_TakesTheLowestFreeNumbersOnTwoToThreeYearDealsAndOnlyReportsMine()
    {
        // Leave number 7 free at my club; everyone else is numbered 2-19 around the keepers' numbers.
        foreach (var club in State.Clubs)
        {
            var squad = State.Players.Where(p => p.Contract?.ClubId == club.Id).ToList();
            var numbers = Enumerable.Range(2, 40).Where(n => n is not (13 or 21) && !(club.Id == 1 && n == 7)).GetEnumerator();
            foreach (var player in squad)
            {
                numbers.MoveNext();
                player.ShirtNumber = numbers.Current;
            }
        }
        var before = State.Players.Select(p => p.Id).ToHashSet();

        AdvanceTo(new DateOnly(2027, 3, 1));

        var intake = State.Players.Where(p => !before.Contains(p.Id)).ToList();
        var mine = intake.Where(p => p.Contract!.ClubId == 1).ToList();
        Assert.All(State.Clubs, c => Assert.Equal(ProcessHelper.YouthIntakePerClub, intake.Count(p => p.Contract!.ClubId == c.Id)));
        Assert.All(intake, p => Assert.Contains(p.Contract!.ExpiryDate, new[] { new DateOnly(2029, 6, 30), new DateOnly(2030, 6, 30) }));

        // 7 is a midfielder's number, so a midfield youngster fills the gap and nobody else takes it.
        bool IsMidfielder(PlayerModel p) => ShirtNumberRules.ReservedNumbersFor(p.Position) == ShirtNumberRules.MidfielderNumbers;
        if (mine.Any(IsMidfielder))
            Assert.Contains(mine, p => p.ShirtNumber == 7 && IsMidfielder(p));
        Assert.DoesNotContain(mine, p => p.ShirtNumber == 7 && !IsMidfielder(p));
        Assert.All(State.Clubs, c =>
        {
            var numbers = State.Players.Where(p => p.Contract?.ClubId == c.Id).Select(p => p.ShirtNumber).ToList();
            Assert.Equal(numbers.Count, numbers.Distinct().Count());
        });

        var notification = Assert.Single(State.Notifications, n => n.Subject == "Youth Intake");
        Assert.All(mine, p => Assert.Contains(p.Name, notification.Message));
        Assert.All(intake.Except(mine), p => Assert.DoesNotContain(p.Name, notification.Message));
    }

    // ----- Physios -----------------------------------------------------------------------

    private static readonly DateOnly PhysioHired = new(2026, 8, 15);

    private PhysioModel HirePhysio(int id) =>
        AddPhysio(new PhysioModel { Id = id, Name = $"Physio {id}", HiredDate = PhysioHired });

    private PhysioModel AddPhysio(PhysioModel physio)
    {
        State.Physios.Add(physio);
        return physio;
    }

    [Fact]
    public void Physios_AreNotPaidBeforeAMonthHasPassed()
    {
        var physio = HirePhysio(1);
        State.Clubs[0].TransferBudget = 1_000_000;

        AdvanceTo(PhysioHired.AddMonths(1).AddDays(-1));

        Assert.Equal(1_000_000, State.Clubs[0].TransferBudget);
        Assert.Equal(0, physio.MonthsWorked);
    }

    [Fact]
    public void Physios_ArePaidFromMyBudgetEachMonth()
    {
        var first = HirePhysio(1);
        var second = HirePhysio(2);
        State.Clubs[0].TransferBudget = 1_000_000;
        var otherBudget = State.Clubs[1].TransferBudget;

        AdvanceTo(PhysioHired.AddMonths(1));

        Assert.Equal(1_000_000 - 2 * PhysioRules.MonthlyWage, State.Clubs[0].TransferBudget);
        Assert.Equal(otherBudget, State.Clubs[1].TransferBudget);
        Assert.All(new[] { first, second }, p => Assert.Equal(1, p.MonthsWorked));
        Assert.Equal(PhysioHired.AddMonths(2), first.NextPaymentDate);
        Assert.Equal(2, State.Physios.Count);
    }

    [Fact]
    public void Physios_ArePaidOnlyOncePerMonth()
    {
        HirePhysio(1);
        State.Clubs[0].TransferBudget = 1_000_000;

        for (var date = PhysioHired.AddMonths(1); date < PhysioHired.AddMonths(2); date = date.AddDays(1))
            AdvanceTo(date);

        Assert.Equal(1_000_000 - PhysioRules.MonthlyWage, State.Clubs[0].TransferBudget);
    }

    [Fact]
    public void Physios_StayWhenTheBudgetCoversTheWageExactly()
    {
        HirePhysio(1);
        State.Clubs[0].TransferBudget = PhysioRules.MonthlyWage;

        AdvanceTo(PhysioHired.AddMonths(1));

        Assert.Equal(0, State.Clubs[0].TransferBudget);
        Assert.Single(State.Physios);
        Assert.DoesNotContain(State.Notifications, n => n.Subject == "Physio Left");
    }

    [Fact]
    public void Physios_LeaveUnpaidWhenTheBudgetCannotCoverTheWage()
    {
        var physio = HirePhysio(1);
        State.Clubs[0].TransferBudget = PhysioRules.MonthlyWage - 1;

        AdvanceTo(PhysioHired.AddMonths(1));

        Assert.Empty(State.Physios);
        Assert.Equal(PhysioRules.MonthlyWage - 1, State.Clubs[0].TransferBudget);
        var notification = Assert.Single(State.Notifications, n => n.Subject == "Physio Left");
        Assert.Equal($"{physio.Name} (Physio)", notification.Recipient);
        Assert.Contains($"£{PhysioRules.MonthlyWage:n0}", notification.Message);
    }

    [Fact]
    public void Physios_WhenOnlyOneWageCanBePaidTheRestLeave()
    {
        HirePhysio(1);
        HirePhysio(2);
        HirePhysio(3);
        State.Clubs[0].TransferBudget = PhysioRules.MonthlyWage + 1;

        AdvanceTo(PhysioHired.AddMonths(1));

        Assert.Equal(1, State.Clubs[0].TransferBudget);
        Assert.Equal(1, Assert.Single(State.Physios).Id);
        Assert.Equal(2, State.Notifications.Count(n => n.Subject == "Physio Left"));
    }

    [Fact]
    public void Physios_HiredOnDifferentDaysArePaidOnTheirOwnDates()
    {
        var early = HirePhysio(1);
        var late = AddPhysio(new PhysioModel { Id = 2, Name = "Physio 2", HiredDate = PhysioHired.AddDays(10) });
        State.Clubs[0].TransferBudget = 1_000_000;

        AdvanceTo(PhysioHired.AddMonths(1));

        Assert.Equal(1, early.MonthsWorked);
        Assert.Equal(0, late.MonthsWorked);
        Assert.Equal(1_000_000 - PhysioRules.MonthlyWage, State.Clubs[0].TransferBudget);
    }

    // ----- Sacking ------------------------------------------------------------------------

    [Theory]
    [InlineData(BoardConfidenceRules.UserSackedBelow, false)]
    [InlineData(BoardConfidenceRules.UserSackedBelow - 1, true)]
    public void IAmSackedOnlyOnceConfidenceFallsBelowTheThreshold(int confidence, bool sacked)
    {
        State.BoardConfidence = confidence;

        AdvanceTo(new DateOnly(2026, 10, 14));

        Assert.Equal(sacked ? (int?)null : 1, State.MyClubId);
        Assert.Equal(sacked, State.Notifications.Any(n => n.Subject.StartsWith("Sacked by")));
    }

    [Fact]
    public void TheGameKeepsAdvancingWhileIAmUnemployed()
    {
        _host.Get<IJobOfferHelper>().SackUser();

        for (var date = new DateOnly(2027, 2, 26); date <= new DateOnly(2027, 3, 3); date = date.AddDays(1))
            AdvanceTo(date);

        Assert.Null(State.MyClubId);
        Assert.Equal(new DateOnly(2027, 3, 3), State.Date);
        Assert.DoesNotContain(State.Notifications, n => n.Subject == "Youth Intake"); // no club, so no report
    }
}
