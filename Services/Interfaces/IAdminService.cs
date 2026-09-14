using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IAdminService
    {
        // =====================================================
        // ACCOUNT CREATION
        // =====================================================

        Task<NewStaffAccountDto> RegisterClinicAdminAsync(RegisterClinicAdminDto dto, Guid performedByUserId);

        Task<NewStaffAccountDto> RegisterNurseAsync(RegisterNurseDto dto, Guid performedByUserId);

        Task<NewStaffAccountDto> RegisterProxyAsync(RegisterProxyDto dto, Guid performedByUserId);

        // =====================================================
        // ACCOUNT MANAGEMENT
        // =====================================================

        Task<List<AdminAccountDto>> ListAccountsAsync(string? role, Guid performedByUserId);

        Task<AdminDashboardDto> GetDashboardAsync(Guid performedByUserId);

        Task DeactivateAccountAsync(Guid userId, Guid performedByUserId);

        Task ActivateAccountAsync(Guid userId, Guid performedByUserId);
        Task<ClinicAdminMeDto> GetClinicAdminMeAsync(Guid performedByUserId);

        Task<ClinicAdminOverviewDto> GetClinicOverviewAsync(Guid performedByUserId);
    }
}