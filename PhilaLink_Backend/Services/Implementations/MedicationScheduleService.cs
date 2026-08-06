using Microsoft.EntityFrameworkCore;
using PhilaLink_Backend.Data;
using PhilaLink_Backend.Models.Entities;
using PhilaLink_Backend.Services.Interfaces;

namespace PhilaLink_Backend.Services.Implementations
{
    public class MedicationScheduleService : IMedicationScheduleService
    {
        private readonly PhilaLinkDbContext _context;

        public MedicationScheduleService(PhilaLinkDbContext context)
        {
            _context = context;
        }

        public async Task<MedicationSchedule> CreateAsync(Guid medicationId, TimeSpan timeOfDay)
        {
            var medication = await _context.Medications.FindAsync(medicationId);
            if (medication == null)
                throw new Exception("Medication not found");

            var schedule = new MedicationSchedule
            {
                Id = Guid.NewGuid(),
                MedicationId = medicationId,
                Medication = medication,
                TimeOfDay = timeOfDay
            };

            _context.MedicationSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return schedule;
        }

        public async Task<List<MedicationSchedule>> GetByMedicationAsync(Guid medicationId)
        {
            return await _context.MedicationSchedules
                .Where(s => s.MedicationId == medicationId)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(Guid scheduleId)
        {
            var schedule = await _context.MedicationSchedules.FindAsync(scheduleId);
            if (schedule == null) return false;

            _context.MedicationSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}