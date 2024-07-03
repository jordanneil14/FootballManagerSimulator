namespace FootballManagerSimulator.Models;

public class Weather
{
    public int MonthNumber { get; set; }
    public IEnumerable<string> WeatherTypes { get; set; } = new List<string>();
    public int MinTemperature { get; set; }
    public int MaxTemperature { get; set; }
}
