namespace FootballManagerSimulator.Maui.Models;

// An application for a vacancy, waiting for the club's decision.
public class JobApplicationModel
{
    public int ClubId { get; set; }
    public DateOnly AppliedOn { get; set; }
    public DateOnly DecideOn { get; set; }
}

// A club that turned you down won't hear from you again until Until.
public class JobRejectionModel
{
    public int ClubId { get; set; }
    public DateOnly Until { get; set; }
}
