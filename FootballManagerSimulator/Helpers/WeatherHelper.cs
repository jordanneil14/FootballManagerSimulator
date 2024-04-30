using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Helpers;

public class WeatherHelper : IWeatherHelper
{
    private readonly IState State;

    private readonly IEnumerable<Weather> Weathers = new List<Weather>
    {
        new Weather
        {
            MonthNumber = 1,
            WeatherTypes = new List<string> { "Snowy", "Frost", "Windy", "Cloudy", "Sunny" },
            MinTemperature = -3,
            MaxTemperature = 3
        },
        new Weather
        {
            MonthNumber = 2,
            WeatherTypes = new List<string> { "Snowy", "Frost", "Windy", "Cloudy", "Sunny" },
            MinTemperature = 0,
            MaxTemperature = 6
        },
        new Weather
        {
            MonthNumber = 3,
            WeatherTypes = new List<string> { "Foggy", "Frost", "Windy", "Cloudy", "Sunny" },
            MinTemperature = 3,
            MaxTemperature = 9
        },
        new Weather
        {
            MonthNumber = 4,
            WeatherTypes = new List<string> { "Foggy", "Frost", "Windy", "Cloudy", "Sunny" },
            MinTemperature = 9,
            MaxTemperature = 15
        },
        new Weather
        {
            MonthNumber = 5,
            WeatherTypes = new List<string> { "Sunny", "Rainy", "Windy", "Cloudy" },
            MinTemperature = 14,
            MaxTemperature = 20
        },
        new Weather
        { 
            MonthNumber = 6,
            WeatherTypes = new List<string> { "Sunny", "Rainy", "Windy", "Cloudy" },
            MinTemperature = 20,
            MaxTemperature = 26
        },
        new Weather
        {
            MonthNumber = 7,
            WeatherTypes = new List<string> { "Sunny", "Rainy", "Windy", "Cloudy" },
            MinTemperature = 20,
            MaxTemperature = 26
        },
        new Weather
        {
            MonthNumber = 8,
            WeatherTypes = new List<string> { "Sunny", "Rainy", "Windy", "Cloudy" },
            MinTemperature = 16,
            MaxTemperature = 22
        },
        new Weather
        {
            MonthNumber = 9,
            WeatherTypes = new List<string> { "Sunny", "Rainy", "Windy", "Cloudy" },
            MinTemperature = 10,
            MaxTemperature = 16
        },
        new Weather
        {
            MonthNumber = 10,
            WeatherTypes = new List<string> { "Sunny", "Rainy", "Windy", "Cloudy" },
            MinTemperature = 6,
            MaxTemperature = 12
        },
        new Weather
        {
            MonthNumber = 11,
            WeatherTypes = new List<string> { "Sunny", "Rainy", "Windy", "Cloudy" },
            MinTemperature = 2,
            MaxTemperature = 8
        },
        new Weather
        {
            MonthNumber = 12,
            WeatherTypes = new List<string> { "Snowy", "Frost", "Windy", "Cloudy", "Sunny" },
            MinTemperature = -2,
            MaxTemperature = 4
        },
    };

    public WeatherHelper(
        IState state)
    {
        State = state;
    }

    public string GetTodaysWeather()
    {
        var month = State.Date.Month;

        var weather = Weathers.First(p => p.MonthNumber == month);

        var weatherType = weather.WeatherTypes.OrderBy(s => Guid.NewGuid()).First();
        var temperature = RandomNumberHelper.Next(weather.MinTemperature, weather.MaxTemperature);

        return $"{weatherType} {temperature}c";
    }
}
