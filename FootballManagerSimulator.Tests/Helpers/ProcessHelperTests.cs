using FootballManagerSimulator.Maui.Events;
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

        Assert.Contains(released, State.Players);
        Assert.Null(released.Contract);
        Assert.Equal(1, stillContracted.Contract!.ClubId);

        var notification = Assert.Single(State.Notifications, n => n.Subject == "Retirements");
        Assert.Contains(myRetiree.Name, notification.Message);
        Assert.DoesNotContain(aiRetiree.Name, notification.Message);
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
        Assert.All(State.Clubs, c => Assert.InRange(intake.Count(p => p.Contract!.ClubId == c.Id), 2, 4));
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
