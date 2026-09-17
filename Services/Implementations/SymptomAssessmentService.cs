using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
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
                SymptomCreateDto dto
            )
        {
            if (
                string.IsNullOrWhiteSpace(
                    dto.Symptoms
                )
            )
            {
                throw new ArgumentException(
                    "Symptoms are required.",
                    nameof(dto.Symptoms)
                );
            }

            if (
                dto.Age.HasValue &&
                (
                    dto.Age.Value < 0 ||
                    dto.Age.Value > 120
                )
            )
            {
                throw new ArgumentException(
                    "Age must be between 0 and 120.",
                    nameof(dto.Age)
                );
            }

            var patient =
                await GetPatientAsync(userId);

            var cleanSymptoms =
                dto.Symptoms.Trim();

            var cleanDuration =
                string.IsNullOrWhiteSpace(
                    dto.Duration
                )
                    ? null
                    : dto.Duration.Trim();

            var allergies =
                CleanList(
                    dto.Allergies
                );

            var medications =
                CleanList(
                    dto.Medications
                );

            var conditions =
                CleanList(
                    dto.Conditions
                );

            var triage =
                AssessSymptoms(
                    cleanSymptoms,
                    dto.Age,
                    cleanDuration,
                    conditions
                );

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
                                    cleanSymptoms,

                                age =
                                    dto.Age,

                                duration =
                                    cleanDuration,

                                allergies,

                                medications,

                                conditions
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
                string symptoms,
                int? age,
                string? duration,
                IReadOnlyCollection<string> conditions
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
                return EmergencyResult();
            }

            if (
                ContainsAny(
                    text,
                    BreathingUrgentKeywords
                )
            )
            {
                return new SymptomTriageResult
                {
                    Result =
                        "Urgent",

                    Recommendation =
                        "Breathing symptoms should be assessed promptly by a healthcare professional. " +
                        "Please contact your clinic, an urgent care service, or another qualified healthcare provider as soon as possible. " +
                        "If you are struggling to breathe, cannot speak normally because of breathlessness, develop chest pain, become confused, faint, or your lips or face turn blue, seek emergency medical help immediately."
                };
            }

            if (
                ContainsAny(
                    text,
                    UrgentKeywords
                )
            )
            {
                return UrgentResult();
            }

            if (
                age.HasValue &&
                (
                    age.Value <= 5 ||
                    age.Value >= 65
                ) &&
                ContainsAny(
                    text,
                    VulnerableAgeKeywords
                )
            )
            {
                return UrgentResult();
            }

            if (
                conditions.Any(
                    condition =>
                        HighRiskConditions.Any(
                            highRisk =>
                                condition.Contains(
                                    highRisk,
                                    StringComparison.OrdinalIgnoreCase
                                )
                        )
                ) &&
                ContainsAny(
                    text,
                    ConditionEscalationKeywords
                )
            )
            {
                return UrgentResult();
            }

            if (
                !string.IsNullOrWhiteSpace(
                    duration
                ) &&
                ContainsAny(
                    duration.ToLowerInvariant(),
                    ProlongedDurationKeywords
                ) &&
                ContainsAny(
                    text,
                    PersistentSymptomKeywords
                )
            )
            {
                return UrgentResult();
            }

            return new SymptomTriageResult
            {
                Result =
                    "NonEmergency",

                Recommendation =
                    "No emergency or urgent warning phrase was detected from the information provided. " +
                    "This is not a diagnosis. Monitor your symptoms, follow your usual care plan, and contact your clinic if symptoms persist, worsen, recur, or concern you. " +
                    "If new severe symptoms develop, seek urgent or emergency medical care."
            };
        }

        private static SymptomTriageResult
            EmergencyResult()
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

        private static SymptomTriageResult
            UrgentResult()
        {
            return new SymptomTriageResult
            {
                Result =
                    "Urgent",

                Recommendation =
                    "These symptoms should be assessed by a healthcare professional soon. " +
                    "Please contact your clinic or another qualified healthcare provider as soon as possible. " +
                    "If symptoms become severe, rapidly worsen, or you develop difficulty breathing, chest pain, fainting, confusion, severe bleeding, or another emergency warning sign, seek emergency medical help immediately."
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

        private static List<string> CleanList(
            IEnumerable<string>? values
        )
        {
            if (values == null)
            {
                return new List<string>();
            }

            return values
                .Where(
                    value =>
                        !string.IsNullOrWhiteSpace(
                            value
                        )
                )
                .Select(
                    value =>
                        value.Trim()
                )
                .Distinct(
                    StringComparer.OrdinalIgnoreCase
                )
                .ToList();
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
            "unable to breathe",
            "not breathing",
            "difficulty breathing",
            "severe shortness of breath",
            "gasping",
            "blue lips",
            "blue face",
            "unconscious",
            "unresponsive",
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
            BreathingUrgentKeywords =
        {
            "shortness of breath",
            "breathless",
            "breathlessness",
            "wheezing",
            "worsening asthma"
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
            "persistent dizziness",
            "fainting",
            "passed out",
            "severe headache",
            "sudden vision loss",
            "sudden loss of vision"
        };

        private static readonly string[]
            VulnerableAgeKeywords =
        {
            "fever",
            "vomiting",
            "diarrhea",
            "dizziness",
            "weakness",
            "cough",
            "infection"
        };

        private static readonly string[]
            HighRiskConditions =
        {
            "asthma",
            "copd",
            "heart",
            "cardiac",
            "diabetes",
            "epilepsy"
        };

        private static readonly string[]
            ConditionEscalationKeywords =
        {
            "worse",
            "worsening",
            "severe",
            "breath",
            "dizzy",
            "faint",
            "vomit",
            "fever"
        };

        private static readonly string[]
            ProlongedDurationKeywords =
        {
            "week",
            "weeks",
            "month",
            "months"
        };

        private static readonly string[]
            PersistentSymptomKeywords =
        {
            "cough",
            "pain",
            "fever",
            "dizziness",
            "vomiting",
            "diarrhea",
            "weakness"
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
