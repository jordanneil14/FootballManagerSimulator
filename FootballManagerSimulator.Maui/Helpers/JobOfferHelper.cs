using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Helpers;

// Which clubs will have you depends on your reputation against their standing. See docs/specs/job-eligibility.md.
public class JobOfferHelper(IState state, INotificationFactory notificationFactory, IReputationHelper reputationHelper) : IJobOfferHelper
{
    // Checked once a week, so a Secure manager gets an approach roughly every seven weeks.
    public const int WeeklyChancePercent = 15;

    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    private readonly IReputationHelper Reputation = reputationHelper;

    // ---------------------------------------------------------------------------------
    // How keen a club is
    // ---------------------------------------------------------------------------------

    // Reputation minus standing, adjusted for how things are going at your current club.
    // Null when the club won't consider you at all (it sacked you recently).
    public double? Gap(ClubModel club, IReadOnlyDictionary<int, double>? standings = null)
    {
        if (SackedBy(club.Id))
            return null;

        standings ??= Reputation.Standings();
        var gap = Reputation.Reputation - standings.GetValueOrDefault(club.Id);

        if (State.MyClubId != null)
        {
            if (State.BoardConfidence < BoardConfidenceRules.StableFrom)
                gap -= JobRules.UnderPressurePenalty;
            else if (State.BoardConfidence >= BoardConfidenceRules.SecureFrom)
                gap += JobRules.SecureBonus;
        }

        return gap;
    }

    public JobChance ChanceFor(ClubModel club, IReadOnlyDictionary<int, double>? standings = null) =>
        Gap(club, standings) is { } gap ? JobRules.ChanceFor(gap) : JobChance.OutOfReach;

    public double AcceptChance(ClubModel club, IReadOnlyDictionary<int, double>? standings = null) =>
        Gap(club, standings) is { } gap ? JobRules.AcceptChance(gap, DaysUnemployed()) : 0;

    // A club that sacked you won't have you back for the rest of that season and all of the next.
    private bool SackedBy(int clubId) => State.ManagerHistory.Jobs.Any(p =>
        p.Sacked && p.ClubId == clubId && p.EndDate is { } end && State.Date <= JobRules.SeasonEnd(end).AddYears(1));

    public int DaysUnemployed()
    {
        if (State.MyClubId != null || State.ManagerHistory.Jobs.LastOrDefault()?.EndDate is not { } since)
            return 0;
        return State.Date.DayNumber - since.DayNumber;
    }

    // ---------------------------------------------------------------------------------
    // Applications
    // ---------------------------------------------------------------------------------

    public JobApplicationModel? ApplicationFor(int clubId) => State.JobApplications.FirstOrDefault(p => p.ClubId == clubId);

    public JobRejectionModel? RejectionFor(int clubId) =>
        State.JobRejections.FirstOrDefault(p => p.ClubId == clubId && State.Date < p.Until);

    // Why you can't apply to the club, or null if you can.
    public string? ApplyRefusal(ClubModel club, IReadOnlyDictionary<int, double>? standings = null)
    {
        if (club.Id == State.MyClubId)
            return "You already manage this club.";
        if (club.VacantUntil == null)
            return "This club isn't looking for a manager.";
        if (ApplicationFor(club.Id) != null)
            return "You've already applied.";
        if (RejectionFor(club.Id) is { } rejection)
            return $"Rejected — reapply from {rejection.Until:d MMM yyyy}.";
        if (SackedBy(club.Id))
            return "They sacked you and won't take you back yet.";
        if (ChanceFor(club, standings) == JobChance.OutOfReach)
            return "Your reputation isn't high enough for this club.";
        if (State.JobApplications.Count >= JobRules.MaxPendingApplications)
            return $"You can only have {JobRules.MaxPendingApplications} applications waiting at once.";
        return null;
    }

    public bool Apply(int clubId)
    {
        var club = State.Clubs.FirstOrDefault(p => p.Id == clubId);
        if (club == null || ApplyRefusal(club) != null)
            return false;

        var decideOn = State.Date.AddDays(RandomNumberHelper.Next(JobRules.DecisionMinDays, JobRules.DecisionMaxDays + 1));
        // Decided while the job is still open: the day before it closes, or tomorrow if it closes then.
        var latest = club.VacantUntil!.Value.AddDays(-1);
        if (decideOn > latest) decideOn = latest;
        if (decideOn <= State.Date) decideOn = State.Date.AddDays(1);

        State.JobApplications.Add(new JobApplicationModel { ClubId = clubId, AppliedOn = State.Date, DecideOn = decideOn });
        return true;
    }

    public void ProcessApplications()
    {
        foreach (var application in State.JobApplications.Where(p => p.DecideOn <= State.Date).ToList())
        {
            State.JobApplications.Remove(application);
            var club = State.Clubs.FirstOrDefault(p => p.Id == application.ClubId);
            if (club == null || club.VacantUntil == null || club.Id == State.MyClubId)
                continue;

            if (RandomNumberHelper.NextDouble() < AcceptChance(club))
                OfferJob(club);
            else
                Reject(club);
        }
    }

    private void OfferJob(ClubModel club)
    {
        // Held open until you answer, which you must do before the next day.
        if (club.VacantUntil <= State.Date)
            club.VacantUntil = State.Date.AddDays(1);

        State.Notifications.Add(new NotificationModel
        {
            Date = State.Date,
            Recipient = $"{club.Name} Chairman",
            Subject = $"{club.Name} want you as manager",
            Message = $"Thank you for your application. We were impressed, and would like you to become the new manager of {club.Name} ({LeagueName(club)}). Will you accept?",
            RequiresAction = true,
            JobOfferClubId = club.Id
        });
    }

    private void Reject(ClubModel club)
    {
        var until = State.Date.AddDays(JobRules.RejectionCooldownDays);
        State.JobRejections.RemoveAll(p => p.ClubId == club.Id);
        State.JobRejections.Add(new JobRejectionModel { ClubId = club.Id, Until = until });

        var reason = (Gap(club) ?? double.MinValue) < JobRules.InterestedFrom
            ? "we're looking for someone with more experience at this level"
            : "we've decided to go in a different direction";
        NotificationFactory.AddNotification(
            State.Date,
            $"{club.Name} Chairman",
            $"Application to {club.Name}",
            $"Thank you for your interest in managing {club.Name}. After careful thought, {reason}. We won't consider another application from you before {until:d MMMM yyyy}.");
    }

    // ---------------------------------------------------------------------------------
    // Unsolicited offers
    // ---------------------------------------------------------------------------------

    public void ProcessJobOffers()
    {
        if (RandomNumberHelper.Next(100) >= WeeklyChancePercent)
            return;

        TryMakeJobOffer();
    }

    public bool TryMakeJobOffer()
    {
        if (State.MyClubId == null || State.BoardConfidence < BoardConfidenceRules.SecureFrom)
            return false;

        if (State.Notifications.Any(p => p.RequiresAction && p.JobOfferClubId != null))
            return false;

        var myClub = State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId);
        if (myClub == null)
            return false;

        // At your level or a step above, never far beyond you or far beneath you.
        var standings = Reputation.Standings();
        var candidates = State.Clubs
            .Where(p => p.Id != myClub.Id
                && Gap(p, standings) is >= JobRules.OfferMinGap and <= JobRules.OfferMaxGap)
            .ToList();

        if (candidates.Count == 0)
            return false;

        // Clubs whose manager is struggling are the likeliest to come calling.
        var club = WeightedPick(candidates, p => BoardConfidenceRules.Max + 1 - p.JobSecurity);

        State.Notifications.Add(new NotificationModel
        {
            Date = State.Date,
            Recipient = $"{club.Name} Chairman",
            Subject = $"Job offer from {club.Name}",
            Message = $"Your work at {myClub.Name} has not gone unnoticed. We would like you to become the new manager of {club.Name} ({LeagueName(club)}). Will you accept?",
            RequiresAction = true,
            JobOfferClubId = club.Id
        });

        return true;
    }

    private static ClubModel WeightedPick(List<ClubModel> clubs, Func<ClubModel, double> weight)
    {
        var total = clubs.Sum(weight);
        var roll = RandomNumberHelper.NextDouble() * total;
        foreach (var club in clubs)
        {
            roll -= weight(club);
            if (roll < 0)
                return club;
        }
        return clubs[^1];
    }

    private string LeagueName(ClubModel club) => State.Competitions.FirstOrDefault(p => p.Id == club.LeagueId)?.Name ?? "";

    // ---------------------------------------------------------------------------------
    // Vacancies
    // ---------------------------------------------------------------------------------

    // Best chance first, then the biggest clubs.
    public IEnumerable<ClubModel> Vacancies()
    {
        var standings = Reputation.Standings();
        return State.Clubs
            .Where(p => p.Id != State.MyClubId && p.VacantUntil != null)
            .OrderBy(p => ChanceFor(p, standings))
            .ThenByDescending(p => standings.GetValueOrDefault(p.Id))
            .ThenBy(p => p.VacantUntil)
            .ThenBy(p => p.Name)
            .ToList();
    }

    public IEnumerable<ClubModel> ManagersUnderThreat() => State.Clubs
        .Where(p => p.Id != State.MyClubId && p.VacantUntil == null && p.JobSecurity < BoardConfidenceRules.UnderPressureFrom)
        .OrderBy(p => p.JobSecurity)
        .ThenBy(p => p.Name);

    // Vacancies you could apply for with a fair chance: Interested or Possible, and not turned down lately.
    public int ReachableVacancies(IReadOnlyDictionary<int, double>? standings = null)
    {
        standings ??= Reputation.Standings();
        return State.Clubs.Count(p => p.Id != State.MyClubId && p.VacantUntil != null && IsReachable(p, standings));
    }

    private bool IsReachable(ClubModel club, IReadOnlyDictionary<int, double> standings) =>
        RejectionFor(club.Id) == null && ChanceFor(club, standings) is JobChance.Interested or JobChance.Possible;

    public void ProcessVacancies()
    {
        foreach (var club in State.Clubs.Where(p => p.VacantUntil != null && p.VacantUntil <= State.Date))
        {
            club.VacantUntil = null;
            club.JobSecurity = BoardConfidenceRules.Starting;

            if (State.JobApplications.RemoveAll(p => p.ClubId == club.Id) > 0)
                NotificationFactory.AddNotification(
                    State.Date,
                    $"{club.Name} Chairman",
                    $"Application to {club.Name}",
                    $"Thank you for applying to manage {club.Name}. The position has now been filled.");

            if (State.ShowManagerNotifications)
                NotificationFactory.AddNotification(
                    State.Date,
                    "Press Officer",
                    $"{club.Name} appoint a new manager",
                    $"{club.Name} have appointed a new manager following the sacking of their previous one.");
        }
    }

    // ---------------------------------------------------------------------------------
    // Out of work
    // ---------------------------------------------------------------------------------

    public void ProcessUnemployment()
    {
        var days = DaysUnemployed();
        if (State.MyClubId != null || days <= 0)
            return;

        if (days > JobRules.DeclineAfterDays && (days - JobRules.DeclineAfterDays) % JobRules.DeclineEveryDays == 0
            && Reputation.Reputation > JobRules.Floor)
            State.ManagerReputation = Math.Max(JobRules.Floor, Reputation.Reputation - JobRules.DeclinePerStep);

        if (days >= JobRules.DeadEndDays)
            OpenReachableVacancy();
    }

    // Nobody who'd have you is hiring, so a struggling club near your level parts with its manager.
    private void OpenReachableVacancy()
    {
        var standings = Reputation.Standings();
        if (ReachableVacancies(standings) > 0)
            return;

        var candidates = State.Clubs
            .Where(p => p.VacantUntil == null && IsReachable(p, standings))
            .ToList();
        var club = candidates
            .Where(p => p.JobSecurity < BoardConfidenceRules.StableFrom)
            .MinBy(p => standings.GetValueOrDefault(p.Id))
            ?? candidates.MinBy(p => p.JobSecurity);
        if (club == null)
            return;

        club.VacantUntil = State.Date.AddDays(
            RandomNumberHelper.Next(BoardConfidenceRules.VacancyMinDays, BoardConfidenceRules.VacancyMaxDays + 1));

        NotificationFactory.AddNotification(
            State.Date,
            "Press Officer",
            $"{club.Name} sack their manager",
            $"{club.Name} have sacked their manager and expect to appoint a replacement by {club.VacantUntil:d MMMM yyyy}. The job is listed on the Jobs screen until then.");
    }

    // ---------------------------------------------------------------------------------
    // Moving clubs
    // ---------------------------------------------------------------------------------

    public void AcceptJobOffer(NotificationModel notification)
    {
        if (!notification.RequiresAction || notification.JobOfferClubId == null)
            return;

        var oldClubName = State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId)?.Name;
        notification.RequiresAction = false;

        if (JoinClub(notification.JobOfferClubId.Value))
        {
            notification.Response = "✅ Job accepted";
            var newClubName = State.Clubs.First(p => p.Id == State.MyClubId).Name;
            notification.Message = oldClubName == null
                ? $"You accepted the job at {newClubName}."
                : $"You accepted the job at {newClubName} and left {oldClubName}.";
        }
        else
        {
            notification.Response = "⚠️ Job no longer available";
        }
    }

    // Only reached through an accepted offer or application. Works whether you are leaving a club or out of work.
    // Leaving for a new job isn't a resignation, so it costs no reputation.
    public bool JoinClub(int clubId)
    {
        var newClub = State.Clubs.FirstOrDefault(p => p.Id == clubId);
        var oldClub = State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId);
        if (newClub == null || newClub.Id == oldClub?.Id)
            return false;

        // Settled before the move, so an old save's reputation comes from the club it was saved at.
        _ = Reputation.Reputation;

        State.MyClubId = newClub.Id;
        State.ManagerHistory.StartJob(newClub, State.Date);
        Reputation.SetExpectedFinish();
        State.BoardConfidence = BoardConfidenceRules.Starting;
        // Both clubs start afresh: the new one with you, the old one with a replacement.
        newClub.JobSecurity = BoardConfidenceRules.Starting;
        newClub.VacantUntil = null;
        State.JobApplications.RemoveAll(p => p.ClubId == newClub.Id);

        if (oldClub != null)
            LeaveClub(oldClub);

        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            $"Welcome to {newClub.Name}",
            $"Everyone at {newClub.Name} wishes you a successful reign as manager. Your budget is {newClub.TransferBudgetFriendly}.");

        return true;
    }

    // The board has run out of patience. The club appoints a replacement straight away, so it never
    // shows up as a vacancy you could walk back into.
    public void SackUser() =>
        BecomeUnemployed(sacked: true, JobRules.Sacked, club => (
            $"Sacked by {club.Name}",
            $"Results have not been good enough and the board has lost confidence in you. You have been relieved of your duties as manager of {club.Name}. Vacant jobs are listed on the Jobs screen."));

    public void ResignUser() =>
        BecomeUnemployed(sacked: false, JobRules.Resigned, club => (
            $"Resignation from {club.Name}",
            $"The board has accepted your resignation as manager of {club.Name} and thanks you for your service. Vacant jobs are listed on the Jobs screen."));

    private void BecomeUnemployed(bool sacked, double reputationChange, Func<ClubModel, (string Subject, string Message)> notification)
    {
        var club = State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId);
        if (club == null)
            return;

        Reputation.Change(reputationChange);
        if (sacked && State.ManagerHistory.CurrentJob is { } job)
            job.Sacked = true;

        State.MyClubId = null;
        State.ManagerHistory.EndJob(State.Date);
        State.BoardConfidence = BoardConfidenceRules.Starting;
        LeaveClub(club);

        var (subject, message) = notification(club);
        var reputation = Reputation.Reputation;
        NotificationFactory.AddNotification(State.Date, $"{club.Name} Chairman", subject,
            $"{message} Your reputation has fallen to {JobRules.Format(reputation)} ({JobRules.StarsText(reputation)}).");
    }

    private void LeaveClub(ClubModel oldClub)
    {
        oldClub.JobSecurity = BoardConfidenceRules.Starting;

        // Requests made at the old club would otherwise pay out to (or rebuild) whichever club you manage next.
        foreach (var @event in State.Events.Where(p => !p.IsCompleted
            && p is IncreaseTransferBudgetRequestEvent or StadiumExpansionEvent or AcademyUpgradeEvent))
        {
            @event.IsCompleted = true;
            oldClub.TransferBudget += RefundFor(@event, oldClub);
        }

        // Scouts, physios and the analyst are on the old club's payroll, so they stay behind.
        State.Scouts.Clear();
        State.Physios.Clear();
        State.Analysts.Clear();

        // Job offers are about you, not the old club, so they still stand.
        foreach (var pending in State.Notifications.Where(p => p.RequiresAction && p.JobOfferClubId == null))
        {
            pending.RequiresAction = false;
            pending.Response = "⚠️ No longer relevant";
            pending.Message += $"\n\nNo longer relevant: you have left {oldClub.Name}.";
        }
    }

    // A cancelled build's money goes back to the club that paid for it. The stadium and academy
    // don't change until the work finishes, so the price is the same as when it was approved.
    private static int RefundFor(IEvent @event, ClubModel club) => @event switch
    {
        StadiumExpansionEvent => StadiumRules.UpgradeCost(club.StadiumSize),
        AcademyUpgradeEvent when club.AcademyRating < AcademyRules.Max => AcademyRules.UpgradeCost(club.AcademyRating + 1),
        _ => 0
    };

    public void DeclineJobOffer(NotificationModel notification)
    {
        if (!notification.RequiresAction || notification.JobOfferClubId == null)
            return;

        var club = State.Clubs.FirstOrDefault(p => p.Id == notification.JobOfferClubId);
        notification.RequiresAction = false;
        notification.Response = "❌ Job declined";
        notification.Message = $"You turned down the job at {club?.Name ?? "the club"}.";
    }
}
