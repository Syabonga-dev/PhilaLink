using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;

namespace PersonalProject.Services.Interfaces
{

    public interface IMedicationService
    {
        Task<Medication> CreateMedicationAsync(
            MedicationCreateDto dto,
            Guid performedByUserId
        );

        Task<List<Medication>>
            GetPatientMedicationsAsync(
                Guid patientId,
                Guid performedByUserId
            );

        Task AddScheduleAsync(
            Guid medicationId,
            string timeOfDay,
            Guid performedByUserId
        );

        Task<List<MedicationLog>>
            GetMedicationLogsAsync(
                Guid medicationId,
                Guid performedByUserId
            );

        Task LogPatientMedicationAsync(
            Guid patientUserId,
            Guid medicationId,
            bool taken,
            string? notes
        );
    }
}