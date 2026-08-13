using PersonalProject.Models.Entities;

namespace PersonalProject.Services.Interfaces
{
    public interface IMedicationService
    {
        Task<Medication> CreateMedicationAsync(Guid patientId, string name, string dosage, string instructions);

        Task<List<Medication>> GetPatientMedicationsAsync(Guid patientId);

        Task<string> AddScheduleAsync(Guid medicationId, string timeOfDay);

        Task<string> LogMedicationAsync(Guid medicationId, bool taken, string? notes);

        Task<List<MedicationLog>> GetMedicationLogsAsync(Guid medicationId);
    }
}

