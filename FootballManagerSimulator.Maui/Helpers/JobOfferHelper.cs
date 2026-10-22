using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Helpers;

public class JobOfferHelper(IState state, INotificationFactory notificationFactory) : IJobOfferHelper
{
    // Checked once a week, so a Secure manager gets an approach roughly every seven weeks.
    public const int WeeklyChancePercent = 15;

    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public void ProcessJobOffers()
    {
        if (RandomNumberHelper.Next(100) >= WeeklyChancePercent)
            return;

        TryMakeJobOffer();
    }

    public bool TryMakeJobOffer()
    {
        if (State.BoardConfidence < BoardConfidenceRules.SecureFrom)
            return false;

        if (State.Notifications.Any(p => p.RequiresAction && p.JobOfferClubId != null))
            return false;

        var myClub = State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId);
        var myLeague = State.Competitions.OfType<LeagueModel>().FirstOrDefault(p => p.Id == myClub?.LeagueId);
        if (myClub == null || myLeague == null)
            return false;

        var eligibleLeagueIds = State.Competitions.OfType<LeagueModel>()
            .Where(p => p.CountryId == myLeague.CountryId && p.Rank <= myLeague.Rank)
            .Select(p => p.Id)
            .ToHashSet();

        var candidates = State.Clubs
            .Where(p => p.Id != myClub.Id && eligibleLeagueIds.Contains(p.LeagueId))
            .ToList();

        if (candidates.Count == 0)
            return false;

        var club = candidates[RandomNumberHelper.Next(candidates.Count)];
        var leagueName = State.Competitions.FirstOrDefault(p => p.Id == club.LeagueId)?.Name ?? "";

        State.Notifications.Add(new NotificationModel
        {
            Date = State.Date,
            Recipient = $"{club.Name} Chairman",
            Subject = $"Job offer from {club.Name}",
            Message = $"Your work at {myClub.Name} has not gone unnoticed. We would like you to become the new manager of {club.Name} ({leagueName}). Will you accept?",
            RequiresAction = true,
            JobOfferClubId = club.Id
        });

        return true;
    }

    public IEnumerable<ClubModel> Vacancies() => State.Clubs
        .Where(p => p.Id != State.MyClubId && p.VacantUntil != null)
        .OrderBy(p => p.VacantUntil)
        .ThenBy(p => p.Name);

    public IEnumerable<ClubModel> ManagersUnderThreat() => State.Clubs
        .Where(p => p.Id != State.MyClubId && p.VacantUntil == null && p.JobSecurity < BoardConfidenceRules.UnderPressureFrom)
        .OrderBy(p => p.JobSecurity)
        .ThenBy(p => p.Name);

    public void ProcessVacancies()
    {
        foreach (var club in State.Clubs.Where(p => p.VacantUntil != null && p.VacantUntil <= State.Date))
        {
            club.VacantUntil = null;
            club.JobSecurity = BoardConfidenceRules.Starting;

            NotificationFactory.AddNotification(
                State.Date,
                "Press Officer",
                $"{club.Name} appoint a new manager",
                $"{club.Name} have appointed a new manager following the sacking of their previous one.");
        }
    }

    public void AcceptJobOffer(NotificationModel notification)
    {
        if (!notification.RequiresAction || notification.JobOfferClubId == null)
            return;

        var oldClubName = State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId)?.Name ?? "your club";
        notification.RequiresAction = false;

        if (JoinClub(notification.JobOfferClubId.Value))
            notification.Message = $"You accepted the job at {State.Clubs.First(p => p.Id == State.MyClubId).Name} and left {oldClubName}.";
    }

    public bool JoinClub(int clubId)
    {
        var newClub = State.Clubs.FirstOrDefault(p => p.Id == clubId);
        var oldClub = State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId);
        if (newClub == null || oldClub == null || newClub.Id == oldClub.Id)
            return false;

        State.MyClubId = newClub.Id;
        State.BoardConfidence = BoardConfidenceRules.Starting;
        // Both clubs start afresh: the new one with you, the old one with a replacement.
        newClub.JobSecurity = BoardConfidenceRules.Starting;
        newClub.VacantUntil = null;
        oldClub.JobSecurity = BoardConfidenceRules.Starting;

        // Requests made at the old club would otherwise pay out to (or rebuild) the new one.
        foreach (var @event in State.Events.Where(p => !p.IsCompleted
            && p is IncreaseTransferBudgetRequestEvent or StadiumExpansionRequestEvent or StadiumExpansionEvent))
            @event.IsCompleted = true;

        foreach (var pending in State.Notifications.Where(p => p.RequiresAction))
        {
            pending.RequiresAction = false;
            pending.Message += $"\n\nNo longer relevant: you have left {oldClub.Name}.";
        }

        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            $"Welcome to {newClub.Name}",
            $"Everyone at {newClub.Name} wishes you a successful reign as manager. Your transfer budget is {newClub.TransferBudgetFriendly}.");

        return true;
    }

    public void DeclineJobOffer(NotificationModel notification)
    {
        if (!notification.RequiresAction || notification.JobOfferClubId == null)
            return;

        var club = State.Clubs.FirstOrDefault(p => p.Id == notification.JobOfferClubId);
        notification.RequiresAction = false;
        notification.Message = $"You turned down the job at {club?.Name ?? "the club"}.";
    }
}
