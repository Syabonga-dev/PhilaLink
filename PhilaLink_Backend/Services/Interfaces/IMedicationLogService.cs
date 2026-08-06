using PhilaLink_Backend.Models.Entities;

namespace PhilaLink_Backend.Services.Interfaces
{
    public interface IMedicationLogService
    {
        Task<MedicationLog> CreateAsync(Guid medicationId, bool taken);
        Task<List<MedicationLog>> GetByMedicationAsync(Guid medicationId);
    }
}