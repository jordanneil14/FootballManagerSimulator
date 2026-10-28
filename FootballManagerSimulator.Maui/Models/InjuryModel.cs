namespace FootballManagerSimulator.Maui.Models;

public class InjuryModel
{
    public int Minute { get; set; }
    public int PlayerId { get; set; }
    public string Description { get; set; } = "";
    public int DaysOut { get; set; }
}
