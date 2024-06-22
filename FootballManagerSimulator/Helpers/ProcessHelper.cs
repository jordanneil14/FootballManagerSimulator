using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Exceptions;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Helpers;

public class ProcessHelper(
    IState state,
    IWeatherHelper weatherHelper,
    ITransferListHelper transferListHelper,
    IEnumerable<ICompetitionFactory> competitionFactories,
    INotificationFactory notificationFactory) : IProcessHelper
{
    public void Process()
    {
        try
        {
            ValidateProcess();
            state.Date = state.Date.AddDays(1);
            state.Weather = weatherHelper.GetTodaysWeather();

            if (state.Date.DayOfWeek == DayOfWeek.Monday)
                transferListHelper.UpdateTransferList();

            transferListHelper.ProcessAITransfers();

            foreach(var comp in state.Competitions)
            {
                var drawDate = comp.DrawDates.FirstOrDefault(p => p.DrawDate == state.Date);
                if (drawDate == null) continue;
                competitionFactories.First(p => p.Type == comp.Type).GenerateNextRoundOfFixtures(comp);

                var clubIds = comp.Clubs.Select(p => p.Id);
                if (clubIds.Any() && clubIds.Contains(state.MyClubId.GetValueOrDefault()))
                {
                    var fixtures = comp.Fixtures.Where(p => p.Round == drawDate.Round);

                    var message = $"Fixtures have been drawn for the next round of the {comp.Name}";
                    var fixtureString = fixtures.Select(p => $"{p.HomeClub.Name} v {p.AwayClub.Name}");
                    message += "\n" + string.Join("\n", fixtureString);

                    notificationFactory.AddNotification(
                        state.Date,
                        "Assistant",
                        $"{comp.Name} Round {drawDate.Round}",
                        message);
                }

            }

            foreach(var comp in state.Competitions)
            {
                var fixture = comp.Fixtures
                    .Where(p => p.Date >= state.Date && (p.HomeClub.Id == state.Clubs.First(p => p.Id == state.MyClubId).Id || p.AwayClub.Id == state.Clubs.First(p => p.Id == state.MyClubId).Id))
                    .OrderBy(p => p.Round)
                    .FirstOrDefault();

                if (fixture != null && fixture.Date.DayNumber == state.Date.DayNumber + 1)
                    competitionFactories.First(p => p.Type == comp.Type).GeneratePreMatchReportForFixture(fixture);

            }
        } catch (ProcessException ex)
        {
            state.ScreenStack.Push(new Structures.Screen
            {
                Type = ex.ScreenType
            });
        }
    }

    private void ValidateProcess()
    {
        var existsOutstandingFixtures = state.Competitions
            .SelectMany(p => p.Fixtures)
            .Any(p => !p.Concluded && p.Date == state.Date);

        if (existsOutstandingFixtures)
            throw new ProcessException(ScreenType.Fixture);
    }
}
