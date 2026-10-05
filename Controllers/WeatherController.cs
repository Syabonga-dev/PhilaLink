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

        /*
         * Weather updates are generated approximately hourly.
         *
         * 55 minutes gives us a small tolerance around the
         * frontend's one-hour refresh interval while still
         * preventing accidental repeat notifications.
         */
        private static readonly TimeSpan
            WeatherNotificationCooldown =
                TimeSpan.FromMinutes(
                    55
                );

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

            var duplicateCutoff =
                DateTime.UtcNow -
                WeatherNotificationCooldown;

            /*
             * FAST DUPLICATE CHECK
             * -------------------------------------------------
             *
             * Do this BEFORE making OpenWeather requests.
             *
             * WeatherChip can legitimately call this endpoint
             * more than once because of page mounting,
             * reconnection or browser lifecycle events.
             *
             * If an hourly notification already exists there is
             * no reason to make another external weather request.
             */
            if (
                await HasRecentWeatherNotificationAsync(
                    userId,
                    duplicateCutoff
                )
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

            /*
             * DATABASE-SERIALIZED INSERT
             * -------------------------------------------------
             *
             * Two requests may still arrive at almost exactly the
             * same time:
             *
             * Request A:
             *   duplicate check -> false
             *
             * Request B:
             *   duplicate check -> false
             *
             * Without serialization they could both insert.
             *
             * PostgreSQL advisory locking serializes the final
             * duplicate-check + insert operation for this patient.
             *
             * This works across requests and across multiple
             * application instances because the lock belongs to
             * PostgreSQL rather than to process memory.
             */
            var notificationCreated =
                await TryCreateWeatherNotificationAsync(
                    userId,
                    message,
                    duplicateCutoff
                );

            return Ok(
                new WeatherTipResultDto
                {
                    NotificationCreated =
                        notificationCreated,

                    Message =
                        message
                }
            );
        }

        // =====================================================
        // DUPLICATE PROTECTION
        // =====================================================

        private Task<bool>
            HasRecentWeatherNotificationAsync(
                Guid userId,
                DateTime duplicateCutoff
            )
        {
            return _context
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
        }

        private async Task<bool>
            TryCreateWeatherNotificationAsync(
                Guid userId,
                string message,
                DateTime duplicateCutoff
            )
        {
            /*
             * pg_advisory_lock accepts a signed 64-bit integer.
             *
             * Derive a stable patient-specific key from the Guid
             * so different patients do not block one another.
             */
            var lockKey =
                CreateWeatherNotificationLockKey(
                    userId
                );

            /*
             * Keep the EF connection open while the PostgreSQL
             * session-level advisory lock is held.
             *
             * SaveChangesAsync will therefore use the same
             * physical PostgreSQL connection.
             */
            await _context.Database
                .OpenConnectionAsync();

            var lockAcquired =
                false;

            try
            {
                await _context.Database
                    .ExecuteSqlInterpolatedAsync(
                        $"SELECT pg_advisory_lock({lockKey});"
                    );

                lockAcquired =
                    true;

                /*
                 * IMPORTANT:
                 * check again AFTER acquiring the lock.
                 *
                 * A competing request may have inserted while
                 * this request was waiting.
                 */
                if (
                    await HasRecentWeatherNotificationAsync(
                        userId,
                        duplicateCutoff
                    )
                )
                {
                    return false;
                }

                _context
                    .Notifications
                    .Add(
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

                return true;
            }
            finally
            {
                if (
                    lockAcquired
                )
                {
                    try
                    {
                        await _context.Database
                            .ExecuteSqlInterpolatedAsync(
                                $"SELECT pg_advisory_unlock({lockKey});"
                            );
                    }
                    catch
                    {
                        /*
                         * If the connection was lost PostgreSQL
                         * automatically releases session advisory
                         * locks when that session terminates.
                         *
                         * Do not hide the original request result
                         * because cleanup itself failed.
                         */
                    }
                }

                await _context.Database
                    .CloseConnectionAsync();
            }
        }

        private static long
            CreateWeatherNotificationLockKey(
                Guid userId
            )
        {
            var bytes =
                userId.ToByteArray();

            /*
             * Mix both halves of the Guid.
             *
             * The constant simply namespaces this advisory key
             * away from other future uses of PostgreSQL advisory
             * locking inside PhilaLink.
             */
            return
                BitConverter.ToInt64(
                    bytes,
                    0
                ) ^
                BitConverter.ToInt64(
                    bytes,
                    8
                ) ^
                0x57454154484552L;
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
