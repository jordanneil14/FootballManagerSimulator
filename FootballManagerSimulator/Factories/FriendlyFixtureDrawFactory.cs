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
    }

    public void CreateEvent()
    {
        DateTime fd = Data.FixtureDate;
        DateOnly fixtureDate = DateOnly.FromDateTime(fd);
        state.Events.Add(new FriendlyFixtureDrawEvent(state)
        {
            FixtureDate = fixtureDate,
            Round = Data.Round
        });
    }
}
