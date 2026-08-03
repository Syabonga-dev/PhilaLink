using PhilaLink_Backend.Models.Entities;

namespace PhilaLink_Backend.Services.Interfaces
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