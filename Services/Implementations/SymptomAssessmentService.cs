using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using System.Text.Json;

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
            if (string.IsNullOrWhiteSpace(symptoms))
            {
                throw new ArgumentException(
                    "Symptoms are required.",
                    nameof(symptoms)
                );
            }

            var patient =
                await GetPatientAsync(userId);

            var cleanSymptoms =
                symptoms.Trim();

            var triage =
                AssessSymptoms(cleanSymptoms);

            var assessment =
                new SymptomAssessment
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        patient.Id,

                    SymptomsJson =
                        JsonSerializer.Serialize(
                            new
                            {
                                symptoms =
                                    cleanSymptoms
                            }
                        ),

                    Result =
                        triage.Result,

                    Recommendation =
                        triage.Recommendation,

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
                await GetPatientAsync(userId);

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

        private static SymptomTriageResult
            AssessSymptoms(
                string symptoms
            )
        {
            var text =
                symptoms.ToLowerInvariant();

            if (
                ContainsAny(
                    text,
                    EmergencyKeywords
                )
            )
            {
                return new SymptomTriageResult
                {
                    Result =
                        "Emergency",

                    Recommendation =
                        "Your symptoms may require immediate medical attention. " +
                        "Please seek emergency medical help now or go to the nearest emergency facility. " +
                        "Do not rely on PhilaLink for emergency treatment."
                };
            }

            if (
                ContainsAny(
                    text,
                    UrgentKeywords
                )
            )
            {
                return new SymptomTriageResult
                {
                    Result =
                        "Urgent",

                    Recommendation =
                        "These symptoms should be assessed by a healthcare professional soon. " +
                        "Please contact your clinic or another qualified healthcare provider. " +
                        "If your symptoms become severe or rapidly worsen, seek emergency medical help."
                };
            }

            return new SymptomTriageResult
            {
                Result =
                        "NonEmergency",

                Recommendation =
                        "No emergency warning signs were detected from the information provided. " +
                        "Monitor your symptoms and contact your clinic if they persist, worsen, " +
                        "or if you are concerned about your health."
            };
        }

        private static bool ContainsAny(
            string text,
            IEnumerable<string> keywords
        )
        {
            return keywords.Any(
                keyword =>
                    text.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase
                    )
            );
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

        private static readonly string[]
            EmergencyKeywords =
        {
            "chest pain",
            "cannot breathe",
            "can't breathe",
            "difficulty breathing",
            "severe shortness of breath",
            "unconscious",
            "not breathing",
            "severe bleeding",
            "bleeding heavily",
            "seizure",
            "convulsion",
            "overdose",
            "anaphylaxis",
            "severe allergic reaction",
            "face drooping",
            "slurred speech",
            "sudden weakness",
            "suicidal",
            "suicide"
        };

        private static readonly string[]
            UrgentKeywords =
        {
            "high fever",
            "persistent fever",
            "vomiting repeatedly",
            "persistent vomiting",
            "severe pain",
            "dehydrated",
            "dehydration",
            "blood in stool",
            "blood in urine",
            "coughing blood",
            "worsening asthma",
            "persistent dizziness",
            "fainting"
        };

        private class SymptomTriageResult
        {
            public string Result { get; set; } =
                string.Empty;

            public string Recommendation { get; set; } =
                string.Empty;
        }
    }
}