using PersonalProject.Models.Entities;

namespace PersonalProject.Services.Interfaces
{
    public interface IMedicationScheduleService
    {
        Task<MedicationSchedule> CreateAsync(Guid medicationId, TimeSpan timeOfDay);
        Task<List<MedicationSchedule>> GetByMedicationAsync(Guid medicationId);
        Task<bool> DeleteAsync(Guid scheduleId);
    }
}

