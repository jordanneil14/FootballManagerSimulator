using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

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

        // Keepers take the free number 1; the first outfield youngster fills the gap at 7.
        if (mine.Any(p => p.Position != "GK"))
            Assert.Contains(mine, p => p.ShirtNumber == 7);
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
