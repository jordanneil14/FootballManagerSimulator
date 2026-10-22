using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Events;

public class AcademyUpgradeEvent(
    IState state,
    INotificationFactory notificationFactory,
    DateOnly triggerDate,
    bool isCompleted) : Event(triggerDate, isCompleted)
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public override void Execute()
    {
        var myClub = State.Clubs.First(p => p.Id == State.MyClubId);
        myClub.AcademyRating = Math.Min(AcademyRules.Max, myClub.AcademyRating + 1);

        NotificationFactory.AddNotification(
            State.Date,
            "Head of Youth Development",
            "Academy Upgrade",
            $"Work on the {myClub.Name} academy is complete. The academy is now rated {myClub.AcademyRating}/{AcademyRules.Max}.");
    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        var inProgress = State.Events.OfType<AcademyUpgradeEvent>().Any(p => !p.IsCompleted);
        return (!inProgress, "An academy upgrade is already underway");
    }
}
