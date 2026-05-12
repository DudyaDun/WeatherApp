using WeatherApp.Models;

namespace WeatherApp.Data;

/// <summary>Root JSON-serialisable container.</summary>
public class WeatherDataStore
{
    public List<WeatherLocation> Locations { get; set; } = new();
    public List<WeatherRecord>   Records   { get; set; } = new();
}
