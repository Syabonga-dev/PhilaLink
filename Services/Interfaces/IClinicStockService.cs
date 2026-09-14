using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IClinicStockService
    {
        Task<List<ClinicStockResponseDto>> GetAllAsync(
            Guid performedByUserId
        );

        Task<ClinicStockResponseDto> CreateAsync(
            CreateClinicStockDto dto,
            Guid performedByUserId
        );

        Task<ClinicStockResponseDto> UpdateAsync(
            Guid stockId,
            UpdateClinicStockDto dto,
            Guid performedByUserId
        );

        Task<ClinicStockResponseDto> AdjustAsync(
            Guid stockId,
            AdjustClinicStockDto dto,
            Guid performedByUserId
        );
    }
}