using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/weather")]
    [Authorize(Policy = "PatientOnly")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;

        public WeatherController(
            IWeatherService weatherService
        )
        {
            _weatherService =
                weatherService;
        }

        [HttpGet("me/current")]
        public async Task<IActionResult>
            GetCurrent(
                [FromQuery] double? latitude = null,
                [FromQuery] double? longitude = null
            )
        {
            var validationError =
                ValidateCoordinates(
                    latitude,
                    longitude
                );

            if (validationError != null)
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

        [HttpGet("me/forecast")]
        public async Task<IActionResult>
            GetForecast(
                [FromQuery] double? latitude = null,
                [FromQuery] double? longitude = null
            )
        {
            var validationError =
                ValidateCoordinates(
                    latitude,
                    longitude
                );

            if (validationError != null)
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

        [HttpPost("me/tips")]
        public async Task<IActionResult>
            GenerateTip(
                [FromQuery] double? latitude = null,
                [FromQuery] double? longitude = null
            )
        {
            var validationError =
                ValidateCoordinates(
                    latitude,
                    longitude
                );

            if (validationError != null)
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
                    .GenerateWeatherTipAsync(
                        GetCurrentUserId(),
                        latitude,
                        longitude
                    )
            );
        }

        private static string?
            ValidateCoordinates(
                double? latitude,
                double? longitude
            )
        {
            if (
                latitude == null &&
                longitude == null
            )
            {
                // No device coordinates supplied.
                // WeatherService will use the
                // patient's assigned clinic.
                return null;
            }

            if (
                latitude == null ||
                longitude == null
            )
            {
                return
                    "Latitude and longitude must be supplied together.";
            }

            if (
                !double.IsFinite(
                    latitude.Value
                ) ||
                latitude.Value < -90 ||
                latitude.Value > 90
            )
            {
                return
                    "Latitude must be between -90 and 90.";
            }

            if (
                !double.IsFinite(
                    longitude.Value
                ) ||
                longitude.Value < -180 ||
                longitude.Value > 180
            )
            {
                return
                    "Longitude must be between -180 and 180.";
            }

            return null;
        }

        private Guid GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
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
                throw new UnauthorizedAccessException();
            }

            return userId;
        }
    }
}