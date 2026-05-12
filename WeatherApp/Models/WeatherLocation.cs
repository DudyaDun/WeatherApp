namespace WeatherApp.Models;

/// <summary>
/// Represents a weather observation location with its metadata.
/// </summary>
public class WeatherLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>City / station name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Latitude in decimal degrees (WGS-84).</summary>
    public double Latitude { get; set; }

    /// <summary>Longitude in decimal degrees (WGS-84).</summary>
    public double Longitude { get; set; }

    /// <summary>Altitude above sea level, metres.</summary>
    public double AltitudeASL { get; set; }

    /// <summary>Data resolution label, e.g. "hourly".</summary>
    public string Resolution { get; set; } = "hourly";

    public override string ToString() => Name;
}
