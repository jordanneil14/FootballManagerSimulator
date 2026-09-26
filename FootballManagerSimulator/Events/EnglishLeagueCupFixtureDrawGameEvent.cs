using FootballManagerSimulator.Competitions.Services;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events
{
    public class EnglishLeagueCupFixtureDrawGameEvent(
    IState state,
    INotificationFactory notificationFactory,
    DateOnly triggerDate,
    EnglishLeagueCupService englishLeagueCupService
        ) : Event(triggerDate)
    {
        private readonly IState State = state;
        private readonly EnglishLeagueCupService EnglishLeagueCupService = englishLeagueCupService;
        private readonly INotificationFactory NotificationFactory = notificationFactory;

        public override void Execute()
        {
            var competition = State.Competitions.First(p => p.Name == "English League Cup");
            var round = competition.Fixtures.Any() ? competition.Fixtures.Max(p => p.Round) + 1 : 1;

            EnglishLeagueCupService.GenerateNextRoundOfFixtures(competition);

            var clubIds = competition.Clubs.Select(p => p.Id);
            if (clubIds.Any() && clubIds.Contains(State.MyClubId.GetValueOrDefault()))
            {
                var fixtures = competition.Fixtures.Where(p => p.Round == round);

                var message = $"Fixtures have been drawn for the next round of the {competition.Name}";
                var fixtureString = fixtures.Select(p => $"{p.HomeClub.Name} v {p.AwayClub.Name}");
                message += "\n" + string.Join("\n", fixtureString);

                NotificationFactory.AddNotification(
                    State.Date,
                    "Assistant",
                    $"{competition.Name} Round {round}",
                    message);
            }
        }

        public override (bool success, string errorMessage) ValidateAdd()
        {
            return (true, "");
        }
    }
}
