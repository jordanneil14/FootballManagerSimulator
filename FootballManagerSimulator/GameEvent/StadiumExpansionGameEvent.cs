using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.GameEvent;

public class StadiumExpansionGameEvent : GameEvent
{
	private readonly IState State;
	private readonly INotificationFactory NotificationFactory;

	public StadiumExpansionGameEvent(
		IState state,
		INotificationFactory notificationFactory,
		DateOnly triggerDate) : base(triggerDate)
	{
		State = state;
		NotificationFactory = notificationFactory;
	}

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
}
