using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Maui.Events;

// Schedules the first draw of each league's play-offs for the day after its last match. Each
// format's draw events schedule the rounds that follow, and the competition end once the last is set.
public static class PlayoffScheduler
{
    public static void Schedule(IState state, IEventManager eventManager, IServiceProvider serviceProvider)
    {
        foreach (var league in state.Competitions.OfType<LeagueModel>().Where(p => p.HasPlayoffEvent))
        {
            // A relegation play-off waits for the league above to finish too.
            var leagues = league.PlayoffFormat == PlayoffFormat.RelegationPlayoff && RelegationPlayoffEvent.LeagueAbove(state, league) is { } leagueAbove
                ? [league, leagueAbove]
                : new[] { league };

            var lastBaseFixtureDate = leagues
                .SelectMany(p => p.Fixtures)
                .Where(p => p.FixtureType == FixtureType.Base)
                .Max(p => p.Date);

            var eventType = league.PlayoffFormat switch
            {
                PlayoffFormat.SerieB => typeof(SerieBPlayoffEvent),
                PlayoffFormat.NationalLeague => typeof(NationalLeaguePlayoffQuarterFinalEvent),
                PlayoffFormat.RelegationPlayoff => typeof(RelegationPlayoffEvent),
                _ => typeof(EnglishFootballLeaguePlayoffEvent)
            };

            var playoffEvent = (IEvent)ActivatorUtilities.CreateInstance(
                serviceProvider, eventType, lastBaseFixtureDate.AddDays(1), false, league.Id);
            eventManager.ValidateAndAddEvent(playoffEvent);
        }
    }
}
