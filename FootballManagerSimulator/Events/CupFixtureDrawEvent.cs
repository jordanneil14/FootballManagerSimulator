using FootballManagerSimulator.Competitions.Services;
using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Factories;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class CupFixtureDrawEvent(DateOnly completionDate, DateOnly fixtureDate, int competitionId, DateOnly startDate, int round) : IEvent
{
    public EventType Type => EventType.CupDrawFixture;
	public DateOnly CompletionDate => completionDate;
    public DateOnly StartDate => startDate;

    public DateOnly FixtureDate { get; set; } = fixtureDate;
    public int Round { get; set; } = round;
    public int CompetitionId { get; set; } = competitionId;

	public void TriggerEvent()
	{
		var cupFixtureDrawEvent = @event as CupFixtureDrawEvent;

		var competition = state.Competitions.First(p => p.Id == cupFixtureDrawEvent.CompetitionId);

		EnglishLeagueCupService.GenerateNextRoundOfFixtures(competition);

		var clubIds = competition.Clubs.Select(p => p.Id);
		if (clubIds.Any() && clubIds.Contains(state.MyClubId.GetValueOrDefault()))
		{
			var fixtures = competition.Fixtures.Where(p => p.Round == cupFixtureDrawEvent.Round);

			var message = $"Fixtures have been drawn for the next round of the {competition.Name}";
			var fixtureString = fixtures.Select(p => $"{p.HomeClub.Name} v {p.AwayClub.Name}");
			message += "\n" + string.Join("\n", fixtureString);

			NotificationFactory.AddNotification(
				state.Date,
				"Assistant",
				$"{competition.Name} Round {cupFixtureDrawEvent.Round}",
				message);
		}
	}
}
