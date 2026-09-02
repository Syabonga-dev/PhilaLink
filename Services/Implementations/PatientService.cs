using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class PatientService : IPatientService
    {
        private readonly PhilaLinkDbContext _context;

        public PatientService(PhilaLinkDbContext context)
        {
            _context = context;
        }

        // Get all patients
        public async Task<List<Patient>> GetAllPatientsAsync()
        {
            return await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Allergies)
                .Include(p => p.MedicalConditions)
                .ToListAsync();
        }

        // Get patient by ID
        public async Task<Patient?> GetPatientByIdAsync(Guid id)
        {
            return await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Allergies)
                .Include(p => p.MedicalConditions)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // Get patient by User ID
        public async Task<Patient?> GetPatientByUserIdAsync(Guid userId)
        {
            return await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Allergies)
                .Include(p => p.MedicalConditions)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        // Create patient
        public async Task<string> CreatePatientAsync(Patient patient)
        {
            var user = await _context.Users.FindAsync(patient.UserId);

            if (user == null)
                return "User not found";

            if (user.Role != "Patient")
                return "User role must be Patient";

            var exists = await _context.Patients
                .AnyAsync(p => p.UserId == patient.UserId);

            if (exists)
                return "Patient already exists";

            patient.Id = Guid.NewGuid();
            patient.CreatedAt = DateTime.UtcNow;
            patient.IsActive = true;

            _context.Patients.Add(patient);

            await _context.SaveChangesAsync();

            return "Patient created successfully";
        }

        // Update patient
        public async Task<string> UpdatePatientAsync(Guid id, Patient patient)
        {
            var existingPatient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingPatient == null)
                return "Patient not found";

            existingPatient.DateOfBirth = patient.DateOfBirth;
            existingPatient.Gender = patient.Gender;
            existingPatient.Email = patient.Email;

            existingPatient.AddressLine1 = patient.AddressLine1;
            existingPatient.AddressLine2 = patient.AddressLine2;
            existingPatient.Suburb = patient.Suburb;
            existingPatient.City = patient.City;
            existingPatient.Province = patient.Province;
            existingPatient.PostalCode = patient.PostalCode;

            existingPatient.EmergencyContactName = patient.EmergencyContactName;
            existingPatient.EmergencyContactPhone = patient.EmergencyContactPhone;
            existingPatient.EmergencyContactRelationship =
                patient.EmergencyContactRelationship;

            existingPatient.IsActive = patient.IsActive;
            existingPatient.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return "Patient updated successfully";
        }

        // Delete patient
        public async Task<string> DeletePatientAsync(Guid id)
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
                return "Patient not found";

            _context.Patients.Remove(patient);

            await _context.SaveChangesAsync();

            return "Patient deleted successfully";
        }
    }
}