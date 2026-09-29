using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
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

        // =====================================================
        // CURRENT WEATHER
        // =====================================================

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

        // =====================================================
        // FORECAST
        // =====================================================

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

        // =====================================================
        // WEATHER HEALTH TIP
        // =====================================================

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
                    NotificationCreated =
                        false,

                    Message =
                        null
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

        // =====================================================
        // OPENWEATHER CURRENT WEATHER
        // =====================================================

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
                await _httpClient.GetAsync(
                    url
                );

            response.EnsureSuccessStatusCode();

            var json =
                await response.Content
                    .ReadAsStringAsync();

            using var document =
                JsonDocument.Parse(
                    json
                );

            var root =
                document.RootElement;

            var main =
                root.GetProperty(
                    "main"
                );

            var wind =
                root.GetProperty(
                    "wind"
                );

            var weather =
                root.GetProperty(
                    "weather"
                )[0];

            string? providerLocationName =
                null;

            if (
                root.TryGetProperty(
                    "name",
                    out var name
                )
            )
            {
                providerLocationName =
                    name.GetString();
            }

            /*
             * OpenWeather occasionally returns the nearest
             * observing station rather than the useful locality.
             *
             * Example:
             *
             * Port Elizabeth Airport
             *
             * The patient-facing PhilaLink location should be:
             *
             * Gqeberha
             *
             * Location normalization is deliberately performed
             * here, before the DTO leaves the weather service,
             * so every consumer receives the same clean name:
             *
             * - WeatherChip
             * - Weather notifications
             * - Future weather features
             */
            var normalizedLocationName =
                NormalizeLocationName(
                    providerLocationName,
                    fallbackName
                );

            return new CurrentWeatherDto
            {
                LocationName =
                    normalizedLocationName,

                TemperatureC =
                    main.GetProperty(
                        "temp"
                    )
                    .GetDouble(),

                FeelsLikeC =
                    main.GetProperty(
                        "feels_like"
                    )
                    .GetDouble(),

                Humidity =
                    main.GetProperty(
                        "humidity"
                    )
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
                    )
                    .GetString()
                    ?? string.Empty,

                ObservedAtUtc =
                    DateTime.UtcNow
            };
        }

        // =====================================================
        // OPENWEATHER FORECAST
        // =====================================================

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
                await _httpClient.GetAsync(
                    url
                );

            response.EnsureSuccessStatusCode();

            var json =
                await response.Content
                    .ReadAsStringAsync();

            using var document =
                JsonDocument.Parse(
                    json
                );

            var results =
                new List<WeatherForecastItemDto>();

            if (
                !document.RootElement
                    .TryGetProperty(
                        "list",
                        out var list
                    )
            )
            {
                return results;
            }

            foreach (
                var item in
                    list
                        .EnumerateArray()
                        .Take(16)
            )
            {
                var main =
                    item.GetProperty(
                        "main"
                    );

                var weather =
                    item.GetProperty(
                        "weather"
                    )[0];

                var wind =
                    item.GetProperty(
                        "wind"
                    );

                var unixTime =
                    item.GetProperty(
                        "dt"
                    )
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
                            main.GetProperty(
                                "temp"
                            )
                            .GetDouble(),

                        FeelsLikeC =
                            main.GetProperty(
                                "feels_like"
                            )
                            .GetDouble(),

                        Humidity =
                            main.GetProperty(
                                "humidity"
                            )
                            .GetInt32(),

                        WindSpeed =
                            wind.TryGetProperty(
                                "speed",
                                out var speed
                            )
                                ? speed.GetDouble()
                                : 0,

                        Description =
                            weather
                                .GetProperty(
                                    "description"
                                )
                                .GetString()
                            ?? string.Empty
                    }
                );
            }

            return results;
        }

        // =====================================================
        // LOCATION RESOLUTION
        // =====================================================

        private async Task<WeatherLocation>
            ResolveWeatherLocationAsync(
                Guid userId,
                double? latitude,
                double? longitude
            )
        {
            /*
             * DEVICE LOCATION PATH
             *
             * When coordinates are supplied we do not need
             * Patient/Clinic entity graphs.
             *
             * We still verify that the Patient account exists,
             * has the Patient role and is active.
             */
            if (
                latitude.HasValue &&
                longitude.HasValue
            )
            {
                ValidateCoordinates(
                    latitude.Value,
                    longitude.Value
                );

                var activePatientExists =
                    await _context.Patients
                        .AsNoTracking()
                        .AnyAsync(
                            patient =>
                                patient.UserId ==
                                    userId &&
                                patient.User.Role ==
                                    RoleNames.Patient &&
                                patient.User.IsActive
                        );

                if (!activePatientExists)
                {
                    throw new
                        UnauthorizedAccessException(
                            "Active patient profile not found."
                        );
                }

                return new WeatherLocation(
                    latitude.Value,
                    longitude.Value,
                    "Current location"
                );
            }

            /*
             * CLINIC FALLBACK PATH
             *
             * No device coordinates were supplied.
             *
             * Retrieve only:
             * - ClinicId
             * - Clinic name
             * - Latitude
             * - Longitude
             */
            var patientLocation =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        patient =>
                            patient.UserId ==
                                userId &&
                            patient.User.Role ==
                                RoleNames.Patient &&
                            patient.User.IsActive
                    )
                    .Select(
                        patient =>
                            new
                            {
                                patient.ClinicId,

                                ClinicName =
                                    patient.Clinic ==
                                        null
                                        ? null
                                        : patient
                                            .Clinic
                                            .Name,

                                Latitude =
                                    patient.Clinic ==
                                        null
                                        ? (double?)null
                                        : patient
                                            .Clinic
                                            .Latitude,

                                Longitude =
                                    patient.Clinic ==
                                        null
                                        ? (double?)null
                                        : patient
                                            .Clinic
                                            .Longitude
                            }
                    )
                    .FirstOrDefaultAsync();

            if (patientLocation == null)
            {
                throw new
                    UnauthorizedAccessException(
                        "Active patient profile not found."
                    );
            }

            if (
                patientLocation.ClinicId ==
                    null ||
                patientLocation.Latitude ==
                    null ||
                patientLocation.Longitude ==
                    null ||
                string.IsNullOrWhiteSpace(
                    patientLocation.ClinicName
                )
            )
            {
                throw new
                    InvalidOperationException(
                        "Current location was not supplied and the patient does not have an assigned clinic for weather fallback."
                    );
            }

            ValidateCoordinates(
                patientLocation
                    .Latitude
                    .Value,
                patientLocation
                    .Longitude
                    .Value
            );

            return new WeatherLocation(
                patientLocation
                    .Latitude
                    .Value,

                patientLocation
                    .Longitude
                    .Value,

                patientLocation
                    .ClinicName
            );
        }

        // =====================================================
        // HUMAN-READABLE LOCATION NAME
        // =====================================================

        private static string
            NormalizeLocationName(
                string? locationName,
                string fallbackName
            )
        {
            /*
             * Prefer the weather provider's locality when one
             * exists. Otherwise use the clinic/current-location
             * fallback supplied by ResolveWeatherLocationAsync.
             */
            var value =
                string.IsNullOrWhiteSpace(
                    locationName
                )
                    ? fallbackName
                    : locationName;

            value =
                CollapseWhitespace(
                    value
                );

            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return
                    "Current location";
            }

            /*
             * Weather providers sometimes identify GPS
             * coordinates by the nearest observation station.
             *
             * Strip those station suffixes so the patient sees
             * a useful locality instead of an airport or
             * meteorological station.
             *
             * Longest suffixes are checked first.
             */
            var stationSuffixes =
                new[]
                {
                    " International Airport",
                    " Meteorological Station",
                    " Weather Station",
                    " Airport",
                    " Airfield",
                    " Aerodrome"
                };

            foreach (
                var suffix in
                    stationSuffixes
            )
            {
                if (
                    !value.EndsWith(
                        suffix,
                        StringComparison
                            .OrdinalIgnoreCase
                    )
                )
                {
                    continue;
                }

                value =
                    value[
                        ..^suffix.Length
                    ]
                    .Trim();

                break;
            }

            /*
             * OpenWeather and some station datasets still use
             * the city's former official name.
             *
             * PhilaLink displays the current city name.
             */
            if (
                value.Equals(
                    "Port Elizabeth",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return
                    "Gqeberha";
            }

            /*
             * Defensive fallback in case the provider returned
             * only a station-style suffix.
             */
            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                value =
                    CollapseWhitespace(
                        fallbackName
                    );
            }

            return
                string.IsNullOrWhiteSpace(
                    value
                )
                    ? "Current location"
                    : value;
        }

        private static string
            CollapseWhitespace(
                string? value
            )
        {
            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return
                    string.Empty;
            }

            return string.Join(
                " ",
                value
                    .Split(
                        (char[]?)null,
                        StringSplitOptions
                            .RemoveEmptyEntries
                    )
            );
        }

        // =====================================================
        // COORDINATE VALIDATION
        // =====================================================

        private static void
            ValidateCoordinates(
                double latitude,
                double longitude
            )
        {
            if (
                !double.IsFinite(
                    latitude
                ) ||
                latitude < -90 ||
                latitude > 90
            )
            {
                throw new
                    ArgumentOutOfRangeException(
                        nameof(latitude),
                        "Latitude must be between -90 and 90."
                    );
            }

            if (
                !double.IsFinite(
                    longitude
                ) ||
                longitude < -180 ||
                longitude > 180
            )
            {
                throw new
                    ArgumentOutOfRangeException(
                        nameof(longitude),
                        "Longitude must be between -180 and 180."
                    );
            }
        }

        // =====================================================
        // COORDINATE FORMATTING
        // =====================================================

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

        // =====================================================
        // HEALTH TIP GENERATION
        // =====================================================

        private static string?
            BuildWeatherTip(
                CurrentWeatherDto current,
                List<WeatherForecastItemDto>
                    forecast
            )
        {
            if (
                current.TemperatureC >=
                    32 ||
                forecast.Any(
                    item =>
                        item.TemperatureC >=
                            32
                )
            )
            {
                return
                    "Weather health tip: Hot conditions are expected. " +
                    "Stay hydrated, avoid prolonged heat exposure, " +
                    "and seek medical help if you develop severe heat-related symptoms.";
            }

            if (
                current.TemperatureC <=
                    8 ||
                forecast.Any(
                    item =>
                        item.TemperatureC <=
                            8
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
                    item =>
                        ContainsWeatherCondition(
                            item.Description,
                            "rain"
                        ) ||
                        ContainsWeatherCondition(
                            item.Description,
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
                current.WindSpeed >=
                    10 ||
                forecast.Any(
                    item =>
                        item.WindSpeed >=
                            10
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

        // =====================================================
        // CONFIGURATION
        // =====================================================

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
                throw new
                    InvalidOperationException(
                        "OpenWeather API key is missing."
                    );
            }

            return apiKey;
        }

        // =====================================================
        // INTERNAL LOCATION MODEL
        // =====================================================

        private sealed record WeatherLocation(
            double Latitude,
            double Longitude,
            string FallbackName
        );
    }
}