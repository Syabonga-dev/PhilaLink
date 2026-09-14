using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class SymptomAssessmentService :
        ISymptomAssessmentService
    {
        private readonly PhilaLinkDbContext _context;

        public SymptomAssessmentService(
            PhilaLinkDbContext context
        )
        {
            _context = context;
        }

        public async Task<SymptomAssessment>
            CreateForPatientAsync(
                Guid userId,
                string symptoms
            )
        {
            var patient =
                await GetPatientAsync(
                    userId
                );

            var assessment =
                new SymptomAssessment
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        patient.Id,

                    SymptomsJson =
                        symptoms,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.SymptomAssessments.Add(
                assessment
            );

            await _context.SaveChangesAsync();

            return assessment;
        }

        public async Task<List<SymptomAssessment>>
            GetMyAssessmentsAsync(
                Guid userId
            )
        {
            var patient =
                await GetPatientAsync(
                    userId
                );

            return await _context
                .SymptomAssessments
                .Where(
                    s =>
                        s.PatientId ==
                        patient.Id
                )
                .OrderByDescending(
                    s => s.CreatedAt
                )
                .ToListAsync();
        }

        private async Task<Patient>
            GetPatientAsync(
                Guid userId
            )
        {
            var patient =
                await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId ==
                                userId &&
                            p.User.Role ==
                                RoleNames.Patient &&
                            p.User.IsActive
                    );

            if (patient == null)
            {
                throw new UnauthorizedAccessException(
                    "Active Patient profile not found."
                );
            }

            return patient;
        }
    }
}