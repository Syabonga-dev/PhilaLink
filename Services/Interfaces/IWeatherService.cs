using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IWeatherService
    {
        Task<CurrentWeatherDto>
            GetCurrentForPatientAsync(Guid userId);

        Task<List<WeatherForecastItemDto>>
            GetForecastForPatientAsync(Guid userId);

        Task<WeatherTipResultDto>
            GenerateWeatherTipAsync(Guid userId);
    }
}