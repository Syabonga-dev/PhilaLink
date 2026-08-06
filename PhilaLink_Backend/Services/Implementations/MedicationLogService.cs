using Microsoft.EntityFrameworkCore;
using PhilaLink_Backend.Data;
using PhilaLink_Backend.Models.Entities;
using PhilaLink_Backend.Services.Interfaces;

namespace PhilaLink_Backend.Services.Implementations
{
    public class MedicationLogService : IMedicationLogService
    {
        private readonly PhilaLinkDbContext _context;

        public MedicationLogService(PhilaLinkDbContext context)
        {
            _context = context;
        }

        public async Task<MedicationLog> CreateAsync(Guid medicationId, bool taken)
        {
            var medication = await _context.Medications.FindAsync(medicationId);
            if (medication == null)
                throw new Exception("Medication not found");

            var log = new MedicationLog
            {
                Id = Guid.NewGuid(),
                MedicationId = medicationId,
                Medication = medication,
                Taken = taken,
                TakenAt = DateTime.UtcNow
            };

            _context.MedicationLogs.Add(log);
            await _context.SaveChangesAsync();

            return log;
        }

        public async Task<List<MedicationLog>> GetByMedicationAsync(Guid medicationId)
        {
            return await _context.MedicationLogs
                .Where(l => l.MedicationId == medicationId)
                .ToListAsync();
        }
    }
}