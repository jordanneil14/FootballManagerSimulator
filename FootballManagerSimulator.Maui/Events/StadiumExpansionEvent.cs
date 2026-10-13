using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class StadiumExpansionEvent(
    IState state,
    INotificationFactory notificationFactory,
    DateOnly triggerDate,
	bool isCompleted) : Event(triggerDate, isCompleted)
{
	private readonly IState State = state;
	private readonly INotificationFactory NotificationFactory = notificationFactory;

    public override void Execute()
	{
		var stadiumSizeIncrease = (int)(State.Clubs.First(p => p.Id == State.MyClubId).StadiumSize * 0.2);
		State.Clubs.First(p => p.Id == State.MyClubId).StadiumSize += stadiumSizeIncrease;

		var myClub = State.Clubs.First(p => p.Id == State.MyClubId);

		NotificationFactory.AddNotification(
			State.Date,
			"Chairman",
			"Stadium Expansion",
			$"{myClub.Name} capactity has been increased by {stadiumSizeIncrease} to {myClub.StadiumSize}");
	}

    public override (bool success, string errorMessage) ValidateAdd()
    {
		return (true, "");
    }
}
