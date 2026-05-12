namespace WeatherApp.Models;

/// <summary>
/// A single hourly weather observation record.
/// </summary>
public class WeatherRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Foreign key to <see cref="WeatherLocation"/>.</summary>
    public Guid LocationId { get; set; }

    /// <summary>Observation timestamp (local time).</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Air temperature at 2 m elevation corrected, °C.</summary>
    public double Temperature { get; set; }

    /// <summary>Relative humidity at 2 m, %.</summary>
    public double RelativeHumidity { get; set; }

    /// <summary>Total cloud cover at surface, %.</summary>
    public double CloudCoverTotal { get; set; }

    /// <summary>
    /// Returns a human-readable comfort index based on temperature and humidity.
    /// </summary>
    public string ComfortIndex
    {
        get
        {
            double hi = Temperature + 0.33 * (RelativeHumidity / 100 * 6.105
                * Math.Exp(17.27 * Temperature / (237.3 + Temperature))) - 4.0;
            return hi switch
            {
                < 10 => "Холодно",
                < 16 => "Прохладно",
                < 24 => "Комфортно",
                < 30 => "Тепло",
                _    => "Жарко"
            };
        }
    }

    /// <summary>Sky condition based on cloud cover.</summary>
    public string SkyCondition => CloudCoverTotal switch
    {
        < 10 => "☀ Ясно",
        < 30 => "🌤 Малооблачно",
        < 70 => "⛅ Переменная облачность",
        < 90 => "🌥 Облачно",
        _    => "☁ Пасмурно"
    };
}
