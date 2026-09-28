using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface INurseService
    {
        Task<NurseMeDto> GetMeAsync(
            Guid userId
        );

        Task<NurseMeDto> UpdateMeAsync(
            Guid userId,
            UpdateNurseProfileDto dto
        );

        Task<NurseDashboardDto> GetDashboardAsync(
            Guid userId
        );

        Task<List<NursePatientDto>>
            GetClinicPatientsAsync(
                Guid userId
            );

        Task<List<NurseAlertDto>>
            GetUrgentAlertsAsync(
                Guid userId
            );

        Task<NursePatientCareDto>
            GetPatientCareAsync(
                Guid userId,
                Guid patientId
            );

        Task<List<NurseClinicProxyDto>>
            GetClinicProxiesAsync(
                Guid userId
            );

        Task<PatientAllergyDto>
            CreateAllergyAsync(
                Guid userId,
                Guid patientId,
                NurseAllergyWriteDto dto
            );

        Task<PatientAllergyDto>
            UpdateAllergyAsync(
                Guid userId,
                Guid patientId,
                Guid allergyId,
                NurseAllergyWriteDto dto
            );

        Task DeleteAllergyAsync(
            Guid userId,
            Guid patientId,
            Guid allergyId
        );

        Task<NurseConditionDto>
            CreateConditionAsync(
                Guid userId,
                Guid patientId,
                NurseConditionWriteDto dto
            );

        Task<NurseConditionDto>
            UpdateConditionAsync(
                Guid userId,
                Guid patientId,
                Guid conditionId,
                NurseConditionWriteDto dto
            );

        Task ArchiveConditionAsync(
            Guid userId,
            Guid patientId,
            Guid conditionId
        );

        Task<NurseCollectionDto>
            ScheduleCollectionAsync(
                Guid userId,
                Guid patientId,
                NurseScheduleCollectionDto dto
            );
    }
}
