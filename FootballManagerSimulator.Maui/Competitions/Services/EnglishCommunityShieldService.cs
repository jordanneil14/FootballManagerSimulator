using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Maui.Competitions.Services;

public class EnglishCommunityShieldService(
    IState state, 
    INotificationFactory notificationFactory) : ICompetitionService
{
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    private readonly IState State = state;

    public void GenerateNextRoundOfFixtures(ICompetition competition)
    {
        var drawSetting = competition.DrawSettings.First();

        var cup = (CupModel)competition;
        cup.Fixtures.Add(new FixtureModel
        {
            HomeClub = competition.Clubs.First(),
            AwayClub = competition.Clubs.Skip(1).First(),
            Date = drawSetting.FixtureDate,
            Round = 1,
            KickOffTime = new TimeOnly(15, 00)
        });
    }

    public void GeneratePreMatchReportForFixture(FixtureModel fixture) =>
        CupPreMatchReport.Send(State, NotificationFactory, fixture);
}
