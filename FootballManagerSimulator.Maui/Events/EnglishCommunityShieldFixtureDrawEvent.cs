using FootballManagerSimulator.Competitions.Services;
using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManagerSimulator.Events;

public class EnglishCommunityShieldFixtureDrawEvent(
    IState state,
    INotificationFactory notificationFactory,
    DateOnly triggerDate,
    bool isCompleted,
    EnglishCommunityShieldService englishCommunityShieldService) : Event(triggerDate, isCompleted)
{
    private readonly IState State = state;
    private readonly EnglishCommunityShieldService EnglishCommunityShieldService = englishCommunityShieldService;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public override void Execute()
    {
        var competition = State.Competitions.First(p => p.Type == CompetitionType.EnglishCommunityShield);

        EnglishCommunityShieldService.GenerateNextRoundOfFixtures(competition);

        var homeClub = competition.Fixtures.First().HomeClub;
        var awayClub = competition.Fixtures.First().AwayClub;

        var message = $"The {competition.Name} fixture between {homeClub.Name} and {awayClub.Name} will take place on {competition.DrawSettings.First().FixtureDate}";
        NotificationFactory.AddNotification(
            State.Date,
            "Journalist",
            competition.Name,
            message);
    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        return (true, "");
    }
}
