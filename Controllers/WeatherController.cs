using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/weather")]
    [Authorize(Policy = "PatientOnly")]
    public class WeatherController :
        ControllerBase
    {
        private static readonly TimeZoneInfo
            SouthAfricaTimeZone =
                ResolveSouthAfricaTimeZone();

        private readonly IWeatherService
            _weatherService;

        private readonly PhilaLinkDbContext
            _context;

        public WeatherController(
            IWeatherService weatherService,
            PhilaLinkDbContext context
        )
        {
            _weatherService =
                weatherService;

            _context =
                context;
        }

        // =====================================================
        // CURRENT
        // =====================================================

        [HttpGet("me/current")]
        public async Task<IActionResult>
            GetCurrent(
                [FromQuery] double?
                    latitude = null,
                [FromQuery] double?
                    longitude = null
            )
        {
            var validationError =
                ValidateCoordinates(
                    latitude,
                    longitude
                );

            if (
                validationError !=
                    null
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            validationError
                    }
                );
            }

            return Ok(
                await _weatherService
                    .GetCurrentForPatientAsync(
                        GetCurrentUserId(),
                        latitude,
                        longitude
                    )
            );
        }

        // =====================================================
        // FORECAST
        // =====================================================

        [HttpGet("me/forecast")]
        public async Task<IActionResult>
            GetForecast(
                [FromQuery] double?
                    latitude = null,
                [FromQuery] double?
                    longitude = null
            )
        {
            var validationError =
                ValidateCoordinates(
                    latitude,
                    longitude
                );

            if (
                validationError !=
                    null
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            validationError
                    }
                );
            }

            return Ok(
                await _weatherService
                    .GetForecastForPatientAsync(
                        GetCurrentUserId(),
                        latitude,
                        longitude
                    )
            );
        }

        // =====================================================
        // WEATHER HEALTH UPDATE
        // =====================================================

        [HttpPost("me/tips")]
        public async Task<IActionResult>
            GenerateTip(
                [FromQuery] double?
                    latitude = null,
                [FromQuery] double?
                    longitude = null
            )
        {
            var validationError =
                ValidateCoordinates(
                    latitude,
                    longitude
                );

            if (
                validationError !=
                    null
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            validationError
                    }
                );
            }

            var userId =
                GetCurrentUserId();

            /*
             * General health/weather updates now respect the
             * saved HealthUpdates preference.
             */
            var patientSettings =
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
                                HealthUpdates =
                                    patient.Preference !=
                                        null &&
                                    patient.Preference
                                        .HealthUpdates
                            }
                    )
                    .FirstOrDefaultAsync();

            if (
                patientSettings ==
                    null
            )
            {
                return Forbid();
            }

            if (
                !patientSettings
                    .HealthUpdates
            )
            {
                return Ok(
                    new WeatherTipResultDto
                    {
                        NotificationCreated =
                            false,

                        Message =
                            null
                    }
                );
            }

            var current =
                await _weatherService
                    .GetCurrentForPatientAsync(
                        userId,
                        latitude,
                        longitude
                    );

            var forecast =
                await _weatherService
                    .GetForecastForPatientAsync(
                        userId,
                        latitude,
                        longitude
                    );

            var message =
                BuildWeatherUpdate(
                    current,
                    forecast
                );

            var duplicateCutoff =
                DateTime.UtcNow
                    .AddMinutes(-55);

            /*
             * Only one weather update is generated inside a
             * roughly one-hour window.
             *
             * This replaces the generic 12-hour suppression that
             * previously made weather notifications appear only
             * once for long periods.
             */
            var recentWeatherNotification =
                await _context
                    .Notifications
                    .AsNoTracking()
                    .AnyAsync(
                        notification =>
                            notification.UserId ==
                                userId &&
                            notification.Message
                                .StartsWith(
                                    "Weather update:"
                                ) &&
                            notification.CreatedAt >=
                                duplicateCutoff
                    );

            if (
                recentWeatherNotification
            )
            {
                return Ok(
                    new WeatherTipResultDto
                    {
                        NotificationCreated =
                            false,

                        Message =
                            message
                    }
                );
            }

            _context.Notifications.Add(
                new Notification
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        userId,

                    Message =
                        message,

                    IsRead =
                        false,

                    CreatedAt =
                        DateTime.UtcNow
                }
            );

            await _context
                .SaveChangesAsync();

            return Ok(
                new WeatherTipResultDto
                {
                    NotificationCreated =
                        true,

                    Message =
                        message
                }
            );
        }

        // =====================================================
        // WEATHER MESSAGE
        // =====================================================

        private static string
            BuildWeatherUpdate(
                CurrentWeatherDto current,
                IReadOnlyCollection<
                    WeatherForecastItemDto
                > forecast
            )
        {
            var now =
                DateTime.UtcNow;

            /*
             * OpenWeather's existing five-day endpoint provides
             * forecast periods at roughly three-hour intervals,
             * not true one-hour forecasts.
             *
             * We therefore use the nearest future forecast
             * period while regenerating the notification hourly.
             */
            var nextForecast =
                forecast
                    .Where(
                        item =>
                            item.ForecastAtUtc >
                                now
                    )
                    .OrderBy(
                        item =>
                            item.ForecastAtUtc
                    )
                    .FirstOrDefault();

            var currentDescription =
                SentenceCase(
                    current.Description
                );

            var currentTemperature =
                Math.Round(
                    current.TemperatureC
                );

            var advice =
                BuildHealthAdvice(
                    current,
                    nextForecast
                );

            if (
                nextForecast ==
                    null
            )
            {
                return
                    "Weather update: " +
                    $"{current.LocationName} is currently " +
                    $"{currentTemperature:0}°C with " +
                    $"{currentDescription}. " +
                    advice;
            }

            var forecastLocalTime =
                ToSouthAfricaTime(
                    nextForecast
                        .ForecastAtUtc
                );

            var forecastTemperature =
                Math.Round(
                    nextForecast
                        .TemperatureC
                );

            var forecastDescription =
                SentenceCase(
                    nextForecast
                        .Description
                );

            return
                "Weather update: " +
                $"{current.LocationName} is currently " +
                $"{currentTemperature:0}°C with " +
                $"{currentDescription}. " +
                $"The next forecast period around " +
                $"{forecastLocalTime:HH:mm} is " +
                $"{forecastTemperature:0}°C with " +
                $"{forecastDescription}. " +
                advice;
        }

        private static string
            BuildHealthAdvice(
                CurrentWeatherDto current,
                WeatherForecastItemDto?
                    forecast
            )
        {
            var highestTemperature =
                Math.Max(
                    current.TemperatureC,
                    forecast
                        ?.TemperatureC ??
                    current.TemperatureC
                );

            if (
                highestTemperature >=
                    32
            )
            {
                return
                    "Stay hydrated and avoid prolonged heat exposure where possible.";
            }

            var lowestTemperature =
                Math.Min(
                    current.TemperatureC,
                    forecast
                        ?.TemperatureC ??
                    current.TemperatureC
                );

            if (
                lowestTemperature <=
                    8
            )
            {
                return
                    "Keep warm and store medication according to its instructions.";
            }

            var combinedDescription =
                $"{current.Description} {forecast?.Description}"
                    .ToLowerInvariant();

            if (
                combinedDescription
                    .Contains(
                        "thunder"
                    ) ||
                combinedDescription
                    .Contains(
                        "storm"
                    )
            )
            {
                return
                    "Stormy conditions are possible; take extra care when travelling and avoid unsafe flooded areas.";
            }

            if (
                combinedDescription
                    .Contains(
                        "rain"
                    ) ||
                combinedDescription
                    .Contains(
                        "drizzle"
                    )
            )
            {
                return
                    "Rain is possible; allow extra travel time for clinic visits or medication collection.";
            }

            var highestWind =
                Math.Max(
                    current.WindSpeed,
                    forecast
                        ?.WindSpeed ??
                    current.WindSpeed
                );

            if (
                highestWind >=
                    10
            )
            {
                return
                    "Strong winds are possible; take extra care when travelling outdoors.";
            }

            return
                "Conditions are relatively mild; keep hydrated and plan ahead if you need to travel for healthcare.";
        }

        // =====================================================
        // VALIDATION
        // =====================================================

        private static string?
            ValidateCoordinates(
                double? latitude,
                double? longitude
            )
        {
            if (
                latitude ==
                    null &&
                longitude ==
                    null
            )
            {
                return null;
            }

            if (
                latitude ==
                    null ||
                longitude ==
                    null
            )
            {
                return
                    "Latitude and longitude must be supplied together.";
            }

            if (
                !double.IsFinite(
                    latitude.Value
                ) ||
                latitude.Value <
                    -90 ||
                latitude.Value >
                    90
            )
            {
                return
                    "Latitude must be between -90 and 90.";
            }

            if (
                !double.IsFinite(
                    longitude.Value
                ) ||
                longitude.Value <
                    -180 ||
                longitude.Value >
                    180
            )
            {
                return
                    "Longitude must be between -180 and 180.";
            }

            return null;
        }

        // =====================================================
        // TIMEZONE
        // =====================================================

        private static TimeZoneInfo
            ResolveSouthAfricaTimeZone()
        {
            try
            {
                return TimeZoneInfo
                    .FindSystemTimeZoneById(
                        "Africa/Johannesburg"
                    );
            }
            catch (
                TimeZoneNotFoundException
            )
            {
                try
                {
                    return TimeZoneInfo
                        .FindSystemTimeZoneById(
                            "South Africa Standard Time"
                        );
                }
                catch
                {
                    return TimeZoneInfo
                        .CreateCustomTimeZone(
                            "PhilaLink-SAST",
                            TimeSpan
                                .FromHours(2),
                            "South Africa Standard Time",
                            "South Africa Standard Time"
                        );
                }
            }
        }

        private static DateTime
            ToSouthAfricaTime(
                DateTime value
            )
        {
            var utc =
                value.Kind ==
                    DateTimeKind.Utc
                    ? value
                    : DateTime
                        .SpecifyKind(
                            value,
                            DateTimeKind.Utc
                        );

            return TimeZoneInfo
                .ConvertTimeFromUtc(
                    utc,
                    SouthAfricaTimeZone
                );
        }

        private static string
            SentenceCase(
                string value
            )
        {
            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return
                    "unknown conditions";
            }

            var clean =
                value.Trim();

            return char
                .ToUpperInvariant(
                    clean[0]
                ) +
                clean[1..];
        }

        // =====================================================
        // CURRENT USER
        // =====================================================

        private Guid
            GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes
                        .NameIdentifier
                );

            if (
                string.IsNullOrWhiteSpace(
                    value
                ) ||
                !Guid.TryParse(
                    value,
                    out var userId
                )
            )
            {
                throw new
                    UnauthorizedAccessException();
            }

            return userId;
        }
    }
}
