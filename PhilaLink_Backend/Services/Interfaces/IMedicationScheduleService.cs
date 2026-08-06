using PhilaLink_Backend.Models.Entities;

namespace PhilaLink_Backend.Services.Interfaces
{
    public interface IMedicationScheduleService
    {
        Task<MedicationSchedule> CreateAsync(Guid medicationId, TimeSpan timeOfDay);
        Task<List<MedicationSchedule>> GetByMedicationAsync(Guid medicationId);
        Task<bool> DeleteAsync(Guid scheduleId);
    }
}