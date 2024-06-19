using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Exceptions;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Helpers;

public class ProcessHelper(
    IState state,
    IWeatherHelper weatherHelper,
    ITransferListHelper transferListHelper,
    IEnumerable<ICompetitionFactory> competitionFactories) : IProcessHelper
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
                if (drawDate != null)
                    competitionFactories.First(p => p.Type == comp.Type).GenerateNextRoundOfFixtures(comp, drawDate.FixtureDate);
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
        var existsOutstandingFixtures = state.TodaysFixtures
            .SelectMany(p => p.Fixtures)
            .Any(p => !p.Concluded);

        if (existsOutstandingFixtures)
            throw new ProcessException(ScreenType.Fixture);
    }
}
