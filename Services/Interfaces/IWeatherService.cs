using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IWeatherService
    {
        Task<CurrentWeatherDto>
            GetCurrentForPatientAsync(
                Guid userId,
                double? latitude = null,
                double? longitude = null
            );

        Task<List<WeatherForecastItemDto>>
            GetForecastForPatientAsync(
                Guid userId,
                double? latitude = null,
                double? longitude = null
            );

        Task<WeatherTipResultDto>
            GenerateWeatherTipAsync(
                Guid userId,
                double? latitude = null,
                double? longitude = null
            );
    }
}