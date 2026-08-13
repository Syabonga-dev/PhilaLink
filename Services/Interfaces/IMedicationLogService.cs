using PersonalProject.Models.Entities;

namespace PersonalProject.Services.Interfaces
{
    public interface IMedicationLogService
    {
        Task<MedicationLog> CreateAsync(Guid medicationId, bool taken);
        Task<List<MedicationLog>> GetByMedicationAsync(Guid medicationId);
    }
}

