using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class MedicationService : IMedicationService
    {
        private readonly PhilaLinkDbContext _context;

        public MedicationService(PhilaLinkDbContext context)
        {
            _context = context;
        }


        public async Task<Medication> CreateMedicationAsync(Guid patientId, string name, string dosage, string instructions)
        {
            var patient = await _context.Users.FindAsync(patientId);
            if (patient == null)
                throw new Exception("Patient not found");

            var medication = new Medication
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Name = name,
                Dosage = dosage,
                Instructions = instructions
            };

            _context.Medications.Add(medication);
            await _context.SaveChangesAsync();

            return medication;
        }

        public async Task<List<Medication>> GetPatientMedicationsAsync(Guid patientId)
        {
            return await _context.Medications
                .Include(m => m.Schedules)
                .Include(m => m.Logs)
                .Where(m => m.PatientId == patientId)
                .ToListAsync();
        }


        public async Task<string> AddScheduleAsync(Guid medicationId, string timeOfDay)
        {
            var medication = await _context.Medications.FindAsync(medicationId);

            if (medication == null)
                return "Medication not found";

            var schedule = new MedicationSchedule
            {
                Id = Guid.NewGuid(),
                MedicationId = medicationId,
                TimeOfDay = TimeSpan.Parse(timeOfDay)
            };

            _context.MedicationSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return "Schedule added successfully";
        }

        public async Task<string> LogMedicationAsync(Guid medicationId, bool taken, string? notes)
        {
            var medication = await _context.Medications.FindAsync(medicationId);

            if (medication == null)
                return "Medication not found";

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

            return "Medication log recorded";
        }


        public async Task<List<MedicationLog>> GetMedicationLogsAsync(Guid medicationId)
        {
            return await _context.MedicationLogs
                .Where(l => l.MedicationId == medicationId)
                .ToListAsync();
        }
    }
}

