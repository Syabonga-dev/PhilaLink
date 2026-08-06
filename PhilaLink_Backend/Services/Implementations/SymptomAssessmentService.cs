using Microsoft.EntityFrameworkCore;
using PhilaLink_Backend.Data;
using PhilaLink_Backend.Models.Entities;
using PhilaLink_Backend.Services.Interfaces;

namespace PhilaLink_Backend.Services.Implementations
{
    public class SymptomAssessmentService : ISymptomAssessmentService
    {
        private readonly PhilaLinkDbContext _context;

        public SymptomAssessmentService(PhilaLinkDbContext context)
        {
            _context = context;
        }

        public async Task<SymptomAssessment> CreateAsync(Guid patientId, string symptoms)
        {
            var patient = await _context.Users.FindAsync(patientId);
            if (patient == null)
                throw new Exception("Patient not found");

            var assessment = new SymptomAssessment
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                SymptomsJson = symptoms,
                CreatedAt = DateTime.UtcNow
            };

            _context.SymptomAssessments.Add(assessment);
            await _context.SaveChangesAsync();

            return assessment;
        }

        public async Task<List<SymptomAssessment>> GetByPatientAsync(Guid patientId)
        {
            return await _context.SymptomAssessments
                .Where(s => s.PatientId == patientId)
                .ToListAsync();
        }
    }
}