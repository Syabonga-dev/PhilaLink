using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IClinicService
    {
        Task<ClinicResponseDto> CreateAsync(
            CreateClinicDto dto
        );

        Task<List<ClinicResponseDto>> GetAllAsync();

        Task<ClinicResponseDto?> GetByIdAsync(Guid id);

        Task<ClinicResponseDto> UpdateAsync(Guid id, UpdateClinicDto dto);

        Task DeactivateAsync(
            Guid id
        );

        Task ActivateAsync(
            Guid id
        );
    }
}