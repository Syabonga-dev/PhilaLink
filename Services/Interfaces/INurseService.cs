using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface INurseService
    {
        Task<NurseMeDto> GetMeAsync(
            Guid userId
        );

        Task<NurseDashboardDto> GetDashboardAsync(
            Guid userId
        );

        Task<List<NursePatientDto>>
            GetClinicPatientsAsync(
                Guid userId
            );
    }
}