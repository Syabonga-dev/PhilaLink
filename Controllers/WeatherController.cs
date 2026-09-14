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
            GetCurrent()
        {
            return Ok(
                await _weatherService
                    .GetCurrentForPatientAsync(
                        GetCurrentUserId()
                    )
            );
        }

        [HttpGet("me/forecast")]
        public async Task<IActionResult>
            GetForecast()
        {
            return Ok(
                await _weatherService
                    .GetForecastForPatientAsync(
                        GetCurrentUserId()
                    )
            );
        }

        [HttpPost("me/tips")]
        public async Task<IActionResult>
            GenerateTip()
        {
            return Ok(
                await _weatherService
                    .GenerateWeatherTipAsync(
                        GetCurrentUserId()
                    )
            );
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