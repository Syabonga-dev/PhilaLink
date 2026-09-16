using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly PhilaLinkDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(
            HttpClient httpClient,
            IConfiguration configuration,
            PhilaLinkDbContext context,
            INotificationService notificationService,
            ILogger<WeatherService> logger
        )
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<CurrentWeatherDto>
            GetCurrentForPatientAsync(
                Guid userId,
                double? latitude = null,
                double? longitude = null
            )
        {
            var location =
                await ResolveWeatherLocationAsync(
                    userId,
                    latitude,
                    longitude
                );

            return await GetCurrentWeatherAsync(
                location.Latitude,
                location.Longitude,
                location.FallbackName
            );
        }

        public async Task<List<WeatherForecastItemDto>>
            GetForecastForPatientAsync(
                Guid userId,
                double? latitude = null,
                double? longitude = null
            )
        {
            var location =
                await ResolveWeatherLocationAsync(
                    userId,
                    latitude,
                    longitude
                );

            return await GetForecastAsync(
                location.Latitude,
                location.Longitude
            );
        }

        public async Task<WeatherTipResultDto>
            GenerateWeatherTipAsync(
                Guid userId,
                double? latitude = null,
                double? longitude = null
            )
        {
            var location =
                await ResolveWeatherLocationAsync(
                    userId,
                    latitude,
                    longitude
                );

            var current =
                await GetCurrentWeatherAsync(
                    location.Latitude,
                    location.Longitude,
                    location.FallbackName
                );

            var forecast =
                await GetForecastAsync(
                    location.Latitude,
                    location.Longitude
                );

            var message =
                BuildWeatherTip(
                    current,
                    forecast
                );

            if (message == null)
            {
                return new WeatherTipResultDto
                {
                    NotificationCreated = false,
                    Message = null
                };
            }

            var created =
                await _notificationService
                    .CreateSystemForPatientAsync(
                        userId,
                        message
                    );

            return new WeatherTipResultDto
            {
                NotificationCreated =
                    created,

                Message =
                    message
            };
        }

        private async Task<CurrentWeatherDto>
            GetCurrentWeatherAsync(
                double latitude,
                double longitude,
                string fallbackName
            )
        {
            var apiKey =
                GetApiKey();

            var url =
                "/data/2.5/weather" +
                $"?lat={FormatCoordinate(latitude)}" +
                $"&lon={FormatCoordinate(longitude)}" +
                $"&appid={Uri.EscapeDataString(apiKey)}" +
                "&units=metric";

            using var response =
                await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var json =
                await response.Content
                    .ReadAsStringAsync();

            using var document =
                JsonDocument.Parse(json);

            var root =
                document.RootElement;

            var main =
                root.GetProperty("main");

            var wind =
                root.GetProperty("wind");

            var weather =
                root.GetProperty("weather")[0];

            return new CurrentWeatherDto
            {
                LocationName =
                    root.TryGetProperty(
                        "name",
                        out var name
                    )
                        ? name.GetString()
                            ?? fallbackName
                        : fallbackName,

                TemperatureC =
                    main.GetProperty("temp")
                        .GetDouble(),

                FeelsLikeC =
                    main.GetProperty("feels_like")
                        .GetDouble(),

                Humidity =
                    main.GetProperty("humidity")
                        .GetInt32(),

                WindSpeed =
                    wind.TryGetProperty(
                        "speed",
                        out var speed
                    )
                        ? speed.GetDouble()
                        : 0,

                Description =
                    weather.GetProperty(
                        "description"
                    ).GetString()
                    ?? string.Empty,

                ObservedAtUtc =
                    DateTime.UtcNow
            };
        }

        private async Task<List<WeatherForecastItemDto>>
            GetForecastAsync(
                double latitude,
                double longitude
            )
        {
            var apiKey =
                GetApiKey();

            var url =
                "/data/2.5/forecast" +
                $"?lat={FormatCoordinate(latitude)}" +
                $"&lon={FormatCoordinate(longitude)}" +
                $"&appid={Uri.EscapeDataString(apiKey)}" +
                "&units=metric";

            using var response =
                await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var json =
                await response.Content
                    .ReadAsStringAsync();

            using var document =
                JsonDocument.Parse(json);

            var results =
                new List<WeatherForecastItemDto>();

            if (
                !document.RootElement.TryGetProperty(
                    "list",
                    out var list
                )
            )
            {
                return results;
            }

            foreach (
                var item in
                list.EnumerateArray().Take(16)
            )
            {
                var main =
                    item.GetProperty("main");

                var weather =
                    item.GetProperty("weather")[0];

                var wind =
                    item.GetProperty("wind");

                var unixTime =
                    item.GetProperty("dt")
                        .GetInt64();

                results.Add(
                    new WeatherForecastItemDto
                    {
                        ForecastAtUtc =
                            DateTimeOffset
                                .FromUnixTimeSeconds(
                                    unixTime
                                )
                                .UtcDateTime,

                        TemperatureC =
                            main.GetProperty("temp")
                                .GetDouble(),

                        FeelsLikeC =
                            main.GetProperty("feels_like")
                                .GetDouble(),

                        Humidity =
                            main.GetProperty("humidity")
                                .GetInt32(),

                        WindSpeed =
                            wind.TryGetProperty(
                                "speed",
                                out var speed
                            )
                                ? speed.GetDouble()
                                : 0,

                        Description =
                            weather.GetProperty(
                                "description"
                            ).GetString()
                            ?? string.Empty
                    }
                );
            }

            return results;
        }

        private async Task<WeatherLocation>
            ResolveWeatherLocationAsync(
                Guid userId,
                double? latitude,
                double? longitude
            )
        {
            var patient =
                await GetPatientAsync(
                    userId
                );

            if (
                latitude.HasValue &&
                longitude.HasValue
            )
            {
                ValidateCoordinates(
                    latitude.Value,
                    longitude.Value
                );

                return new WeatherLocation(
                    latitude.Value,
                    longitude.Value,
                    "Current location"
                );
            }

            if (
                patient.ClinicId == null ||
                patient.Clinic == null
            )
            {
                throw new InvalidOperationException(
                    "Current location was not supplied and the patient does not have an assigned clinic for weather fallback."
                );
            }

            ValidateCoordinates(
                patient.Clinic.Latitude,
                patient.Clinic.Longitude
            );

            return new WeatherLocation(
                patient.Clinic.Latitude,
                patient.Clinic.Longitude,
                patient.Clinic.Name
            );
        }

        private static void
            ValidateCoordinates(
                double latitude,
                double longitude
            )
        {
            if (
                !double.IsFinite(latitude) ||
                latitude < -90 ||
                latitude > 90
            )
            {
                throw new ArgumentOutOfRangeException(
                    nameof(latitude),
                    "Latitude must be between -90 and 90."
                );
            }

            if (
                !double.IsFinite(longitude) ||
                longitude < -180 ||
                longitude > 180
            )
            {
                throw new ArgumentOutOfRangeException(
                    nameof(longitude),
                    "Longitude must be between -180 and 180."
                );
            }
        }

        private static string
            FormatCoordinate(
                double value
            )
        {
            return value.ToString(
                "0.######",
                CultureInfo.InvariantCulture
            );
        }

        private static string?
            BuildWeatherTip(
                CurrentWeatherDto current,
                List<WeatherForecastItemDto> forecast
            )
        {
            if (
                current.TemperatureC >= 32 ||
                forecast.Any(
                    f => f.TemperatureC >= 32
                )
            )
            {
                return
                    "Weather health tip: Hot conditions are expected. " +
                    "Stay hydrated, avoid prolonged heat exposure, " +
                    "and seek medical help if you develop severe heat-related symptoms.";
            }

            if (
                current.TemperatureC <= 8 ||
                forecast.Any(
                    f => f.TemperatureC <= 8
                )
            )
            {
                return
                    "Weather health tip: Cold conditions are expected. " +
                    "Keep warm, especially if you are vulnerable to cold weather, " +
                    "and keep medicines stored according to their instructions.";
            }

            var rainExpected =
                ContainsWeatherCondition(
                    current.Description,
                    "rain"
                ) ||
                forecast.Any(
                    f =>
                        ContainsWeatherCondition(
                            f.Description,
                            "rain"
                        ) ||
                        ContainsWeatherCondition(
                            f.Description,
                            "thunderstorm"
                        )
                );

            if (rainExpected)
            {
                return
                    "Weather health tip: Rain or storms are expected. " +
                    "Plan extra travel time for clinic appointments or medicine collection " +
                    "and avoid unsafe flooded areas.";
            }

            if (
                current.WindSpeed >= 10 ||
                forecast.Any(
                    f => f.WindSpeed >= 10
                )
            )
            {
                return
                    "Weather health tip: Strong winds are expected. " +
                    "Take extra care when travelling and avoid unnecessary exposure " +
                    "if conditions become unsafe.";
            }

            return null;
        }

        private static bool
            ContainsWeatherCondition(
                string description,
                string value
            )
        {
            return description.Contains(
                value,
                StringComparison.OrdinalIgnoreCase
            );
        }

        private string GetApiKey()
        {
            var apiKey =
                _configuration[
                    "Weather:ApiKey"
                ];

            if (
                string.IsNullOrWhiteSpace(
                    apiKey
                )
            )
            {
                throw new InvalidOperationException(
                    "OpenWeather API key is missing."
                );
            }

            return apiKey;
        }

        private async Task<Patient>
            GetPatientAsync(
                Guid userId
            )
        {
            var patient =
                await _context.Patients
                    .Include(p => p.User)
                    .Include(p => p.Clinic)
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId == userId &&
                            p.User.Role ==
                                RoleNames.Patient &&
                            p.User.IsActive
                    );

            if (patient == null)
            {
                throw new UnauthorizedAccessException(
                    "Active patient profile not found."
                );
            }

            return patient;
        }

        private sealed record WeatherLocation(
            double Latitude,
            double Longitude,
            string FallbackName
        );
    }
}