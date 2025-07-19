using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public class FriendlyFixtureDrawFactory(
    IState state,
    IEnumerable<ICompetitionFactory> competitionFactories,
    INotificationFactory notificationFactory) : IEventFactory
{
    public EventType Type => EventType.FriendlyDrawFixture;

    public dynamic Data { get; set; } = new JObject();

    public void CompleteEvent(IEvent @event)
    {
        var cupFixtureDrawEvent = @event as FriendlyFixtureDrawEvent;

        var comp = state.Competitions.First(p => p.Type == CompetitionType.Friendly);

        competitionFactories.First(p => p.Type == CompetitionType.Friendly).GenerateNextRoundOfFixtures(comp);

        var competition = competitionFactories.First(p => p.Type == CompetitionType.Friendly);

        var fixture = comp.Fixtures.First(p => (p.HomeClub.Id == state.MyClubId || p.AwayClub.Id == state.MyClubId) && p.Round == cupFixtureDrawEvent.Round);
        var oppositionClubName = fixture.HomeClub.Id == state.MyClubId ? fixture.AwayClub.Name : fixture.HomeClub.Name;
        var date = fixture.Date;

        var message = $"A friendly has been arranged against {oppositionClubName} on {date}";

        notificationFactory.AddNotificationNow(
            "Chairman",
            "Friendly Arranged",
            message);
    }

    public void CreateEvent()
    {
        DateTime fd = Data.FixtureDate;
        DateOnly fixtureDate = DateOnly.FromDateTime(fd);
        state.Events.Add(new FriendlyFixtureDrawEvent(state)
        {
            FixtureDate = fixtureDate,
            Round = Data.Round,
            
        });
    }
}
