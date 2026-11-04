namespace FootballManagerSimulator.Maui.Models;

public class SubstitutionModel
{
    public int Minute { get; set; }
    // 0 when a player came on into an empty slot (friendlies).
    public int OffId { get; set; }
    public int OnId { get; set; }
}
