using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public class CupFixtureDrawFactory(
    IState state,
    IEnumerable<ICompetitionFactory> competitionFactories,
    INotificationFactory notificationFactory) : IEventFactory
{
    public EventType Type => EventType.CupDrawFixture;

    public dynamic Data { get; set; } = new JObject();
    public DateOnly CompletionDate { get; set; }

    public void CompleteEvent(IEvent @event)
    {
        var cupFixtureDrawEvent = @event as CupFixtureDrawEvent;

        var comp = state.Competitions.First(p => p.Id == cupFixtureDrawEvent.CompetitionId);

        competitionFactories.First(p => p.Type == comp.Type).GenerateNextRoundOfFixtures(comp);

        var clubIds = comp.Clubs.Select(p => p.Id);
        if (clubIds.Any() && clubIds.Contains(state.MyClubId.GetValueOrDefault()))
        {
            var fixtures = comp.Fixtures.Where(p => p.Round == cupFixtureDrawEvent.Round);

            var message = $"Fixtures have been drawn for the next round of the {comp.Name}";
            var fixtureString = fixtures.Select(p => $"{p.HomeClub.Name} v {p.AwayClub.Name}");
            message += "\n" + string.Join("\n", fixtureString);

            notificationFactory.AddNotification(
                state.Date,
                "Assistant",
                $"{comp.Name} Round {cupFixtureDrawEvent.Round}",
                message);
        }
    }

    public void CreateEvent()
    {
        DateTime dd = Data.DrawDate;
        DateTime fd = Data.FixtureDate;
        DateOnly drawDate = DateOnly.FromDateTime(dd);
        DateOnly fixtureDate = DateOnly.FromDateTime(fd);
        state.Events.Add(new CupFixtureDrawEvent(state)
        {
            DrawDate = drawDate,
            FixtureDate = fixtureDate,
            Round = Data.Round,
            CompetitionId = Data.CompetitionId
        });
    }
}
