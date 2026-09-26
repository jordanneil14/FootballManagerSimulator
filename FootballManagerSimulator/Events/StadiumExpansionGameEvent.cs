using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class StadiumExpansionGameEvent(
    IState state,
    INotificationFactory notificationFactory,
    DateOnly triggerDate) : Event(triggerDate)
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
