using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Services;

public class EnglishFACupService(INotificationFactory notificationFactory, IState state) : ICompetitionService
{
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    private readonly IState State = state;

    // An open draw of last round's winners and this round's new entrants; the first club drawn is at home.
    public void GenerateNextRoundOfFixtures(ICompetition competition)
    {
        var cup = (CupModel)competition;
        var drawSettings = cup.DrawSettings.First(p => p.Round == cup.Round);

        var winners = cup.Fixtures
            .Where(p => p.Round == cup.Round - 1 && p.ClubWon != null)
            .Select(p => p.ClubWon!.Id);
        var clubIds = winners.Concat(drawSettings.IntroducedClubIds).ToHashSet();

        var clubs = cup.Clubs
            .Where(p => clubIds.Contains(p.Id))
            .OrderBy(_ => RandomNumberHelper.Next())
            .ToList();

        if (clubs.Count % 2 != 0)
            throw new InvalidOperationException($"{cup.Name} round {cup.Round} has an odd number of clubs ({clubs.Count}).");

        for (var i = 0; i < clubs.Count; i += 2)
        {
            cup.Fixtures.Add(new FixtureModel
            {
                HomeClub = clubs[i],
                AwayClub = clubs[i + 1],
                Date = drawSettings.FixtureDate,
                Round = cup.Round,
                KickOffTime = FACupRules.KickOff
            });
        }
    }

    public void GeneratePreMatchReportForFixture(FixtureModel fixture) =>
        CupPreMatchReport.Send(State, NotificationFactory, fixture);
}
