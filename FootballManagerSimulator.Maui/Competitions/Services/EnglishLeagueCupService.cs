using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Maui.Competitions.Services;

public class EnglishLeagueCupService(
    IOptions<SettingsModel> settings,
    INotificationFactory notificationFactory,
    IState state) : ICompetitionService
{
    private readonly SettingsModel Settings = settings.Value;
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    private readonly IState State = state;

    public void GenerateNextRoundOfFixtures(ICompetition competition)
    {
        var cup = (CupModel)competition;

        var drawDate = cup.DrawSettings.First(p => p.Round == cup.Round);

        if (cup.Round == 1)
        {
            var leagues = Settings.Competitions.Where(p => p.Type == CompetitionType.EnglishFootballLeague.ToString());
            var leagueIds = leagues.Select(p => p.Id);

            var clubs = cup.Clubs.Where(p => leagueIds.Contains(p.LeagueId));

            cup.Fixtures = GenerateFixtures(clubs, drawDate.FixtureDate, cup.Round);
            return;
        }

        IEnumerable<FixtureModel> lastRoundOfFixtures = cup.Fixtures.Where(p => p.Round == cup.Round - 1);

        var winningClubs = lastRoundOfFixtures
            .Where(p => p.ClubWon != null)
            .Select(p => p.ClubWon)
            .Cast<ClubModel>();
        var includedClubs = drawDate.IntroducedClubIds == null ? [] : cup.Clubs.Where(p => drawDate.IntroducedClubIds.Contains(p.Id));
        var nextRoundClubs = winningClubs.Concat(includedClubs);

        cup.Fixtures.AddRange(GenerateFixtures(nextRoundClubs, drawDate.FixtureDate, cup.Round));
    }

    private List<FixtureModel> GenerateFixtures(IEnumerable<ClubModel> clubs, DateOnly date, int round)
    {
        var clubList = clubs.OrderBy(_ => RandomNumberHelper.Next()).ToList();
        if (clubList.Count % 2 != 0)
            clubList.RemoveAt(clubList.Count - 1);

        var fixtures = new List<FixtureModel>();
        for (var i = 0; i < clubList.Count; i += 2)
        {
            fixtures.Add(new FixtureModel()
            {
                HomeClub = clubList[i],
                AwayClub = clubList[i + 1],
                Date = date,
                Round = round,
                KickOffTime = new TimeOnly(19, 45)
            });
        }
        return fixtures;
    }

    public void GeneratePreMatchReportForFixture(FixtureModel fixture)
    {
        NotificationFactory.AddNotification(
            State.Date,
            "Club Analyst",
            "Pre-Match Report",
            "English league Cup match incoming");
    }
}
