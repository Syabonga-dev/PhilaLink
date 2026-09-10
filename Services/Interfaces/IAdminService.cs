using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IAdminService
    {
        Task<NewStaffAccountDto> RegisterNurseAsync(RegisterNurseDto dto);
        Task<NewStaffAccountDto> RegisterProxyAsync(RegisterProxyDto dto);

        Task<List<AdminAccountDto>> ListAccountsAsync(string? role);

        Task<AdminDashboardDto> GetDashboardAsync();

        Task DeactivateAccountAsync(Guid userId);
        Task ActivateAccountAsync(Guid userId);
        Task DeleteAccountAsync(Guid userId);
    }
}