namespace PersonalProject.Models.DTOs
{
    public class CurrentWeatherDto
    {
        public string LocationName { get; set; } = string.Empty;
        public double TemperatureC { get; set; }
        public double FeelsLikeC { get; set; }
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime ObservedAtUtc { get; set; }
    }

    public class WeatherForecastItemDto
    {
        public DateTime ForecastAtUtc { get; set; }
        public double TemperatureC { get; set; }
        public double FeelsLikeC { get; set; }
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class WeatherTipResultDto
    {
        public bool NotificationCreated { get; set; }
        public string? Message { get; set; }
    }
}