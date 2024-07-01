using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class StadiumExpansionEvent(IState state, INotificationFactory notificationFactory) : BaseEvent(state)
{
    public override EventType Event => EventType.StadiumExpansion;

    private DateOnly completionDate;
    public override DateOnly CompletionDate => completionDate;

    public override void Complete()
    {
        var stadiumSizeIncrease = (int)(state.Clubs.First(p => p.Id == state.MyClubId).StadiumSize * 0.2);
        state.Clubs.First(p => p.Id == state.MyClubId).StadiumSize += stadiumSizeIncrease;

        var myClub = state.Clubs.First(p => p.Id == state.MyClubId);

        notificationFactory.AddNotification(
            state.Date,
            "Chairman",
            "Stadium Expansion",
            $"{myClub.Name} capactity has been increased by {stadiumSizeIncrease} to {myClub.StadiumSize}");
    }

    public override void Start()
    {
        completionDate = state.Date.AddMonths(3);
        base.Initialise();
    }
}
