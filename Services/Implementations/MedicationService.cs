using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class MedicationService :
        IMedicationService
    {
        private readonly PhilaLinkDbContext _context;
        private readonly IAuditLogService _audit;

        public MedicationService(
            PhilaLinkDbContext context,
            IAuditLogService audit
        )
        {
            _context = context;
            _audit = audit;
        }

        // =====================================================
        // CLINIC STAFF: CREATE
        // =====================================================

        public async Task<Medication>
            CreateMedicationAsync(
                MedicationCreateDto dto,
                Guid performedByUserId
            )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            var patient =
                await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id == dto.PatientId &&
                            p.User.IsActive
                    );

            if (patient == null)
            {
                throw new KeyNotFoundException(
                    "Patient not found."
                );
            }

            if (
                patient.ClinicId !=
                clinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "Patient does not belong to your clinic."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    dto.Name
                ) ||
                string.IsNullOrWhiteSpace(
                    dto.Dosage
                )
            )
            {
                throw new InvalidOperationException(
                    "Medication name and dosage are required."
                );
            }

            if (
                dto.UnitsPerDose != null &&
                dto.UnitsPerDose <= 0
            )
            {
                throw new InvalidOperationException(
                    "Units per dose must be greater than zero."
                );
            }

            var medication =
                new Medication
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        patient.Id,

                    Name =
                        dto.Name.Trim(),

                    Dosage =
                        dto.Dosage.Trim(),

                    Form =
                        dto.Form.Trim(),

                    Instructions =
                        dto.Instructions.Trim(),

                    UnitsPerDose =
                        dto.UnitsPerDose,

                    PrescribedBy =
                        string.IsNullOrWhiteSpace(
                            dto.PrescribedBy
                        )
                            ? null
                            : dto.PrescribedBy.Trim(),

                    ConditionName =
                        string.IsNullOrWhiteSpace(
                            dto.ConditionName
                        )
                            ? null
                            : dto.ConditionName.Trim(),

                    StartDate =
                        dto.StartDate == default
                            ? DateTime.UtcNow
                            : dto.StartDate,

                    EndDate =
                        dto.EndDate,

                    IsActive =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.Medications.Add(
                medication
            );

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "MedicationCreated",
                performedByUserId,
                $"Medication {medication.Id} created for patient {patient.Id}.",
                clinicId
            );

            return medication;
        }

        // =====================================================
        // PATIENT: OWN MEDICATIONS
        // =====================================================

        public async Task<List<Medication>>
            GetPatientMedicationsByUserIdAsync(
                Guid patientUserId
            )
        {
            var patient =
                await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId ==
                                patientUserId &&
                            p.User.Role ==
                                RoleNames.Patient &&
                            p.User.IsActive
                    );

            if (patient == null)
            {
                throw new KeyNotFoundException(
                    "Active patient profile not found."
                );
            }

            return await _context
                .Medications
                .Include(m => m.Schedules)
                .Include(m => m.Logs)
                .Where(
                    m =>
                        m.PatientId ==
                        patient.Id
                )
                .OrderByDescending(
                    m => m.IsActive
                )
                .ThenBy(
                    m => m.Name
                )
                .ToListAsync();
        }

        // =====================================================
        // CLINIC STAFF: READ PATIENT MEDICATIONS
        // =====================================================

        public async Task<List<Medication>>
            GetPatientMedicationsAsync(
                Guid patientId,
                Guid performedByUserId
            )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            patientId
                    );

            if (patient == null)
            {
                throw new KeyNotFoundException(
                    "Patient not found."
                );
            }

            if (
                patient.ClinicId !=
                clinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "Patient does not belong to your clinic."
                );
            }

            return await _context
                .Medications
                .Include(m => m.Schedules)
                .Include(m => m.Logs)
                .Where(
                    m =>
                        m.PatientId ==
                        patientId
                )
                .OrderBy(
                    m => m.Name
                )
                .ToListAsync();
        }

        // =====================================================
        // CLINIC STAFF: SCHEDULE
        // =====================================================

        public async Task AddScheduleAsync(
            Guid medicationId,
            string timeOfDay,
            Guid performedByUserId
        )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            var medication =
                await _context.Medications
                    .Include(
                        m => m.Patient
                    )
                    .FirstOrDefaultAsync(
                        m =>
                            m.Id ==
                            medicationId
                    );

            if (medication == null)
            {
                throw new KeyNotFoundException(
                    "Medication not found."
                );
            }

            if (
                medication.Patient.ClinicId !=
                clinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "Medication belongs to a patient outside your clinic."
                );
            }

            if (
                !TimeSpan.TryParse(
                    timeOfDay,
                    out var scheduleTime
                )
            )
            {
                throw new InvalidOperationException(
                    "Invalid medication schedule time."
                );
            }

            var duplicate =
                await _context
                    .MedicationSchedules
                    .AnyAsync(
                        s =>
                            s.MedicationId ==
                                medicationId &&
                            s.TimeOfDay ==
                                scheduleTime &&
                            s.IsActive
                    );

            if (duplicate)
            {
                throw new InvalidOperationException(
                    "That medication schedule already exists."
                );
            }

            var schedule =
                new MedicationSchedule
                {
                    Id =
                        Guid.NewGuid(),

                    MedicationId =
                        medicationId,

                    TimeOfDay =
                        scheduleTime,

                    IsActive =
                        true
                };

            _context
                .MedicationSchedules
                .Add(
                    schedule
                );

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "MedicationScheduleAdded",
                performedByUserId,
                $"Schedule added to medication {medicationId}.",
                clinicId
            );
        }

        // =====================================================
        // CLINIC STAFF: LOG HISTORY
        // =====================================================

        public async Task<List<MedicationLog>>
            GetMedicationLogsAsync(
                Guid medicationId,
                Guid performedByUserId
            )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            var medication =
                await _context.Medications
                    .Include(
                        m => m.Patient
                    )
                    .FirstOrDefaultAsync(
                        m =>
                            m.Id ==
                            medicationId
                    );

            if (medication == null)
            {
                throw new KeyNotFoundException(
                    "Medication not found."
                );
            }

            if (
                medication.Patient.ClinicId !=
                clinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "Medication belongs to a patient outside your clinic."
                );
            }

            return await _context
                .MedicationLogs
                .Where(
                    l =>
                        l.MedicationId ==
                        medicationId
                )
                .OrderByDescending(
                    l => l.TakenAt
                )
                .ToListAsync();
        }

        // =====================================================
        // PATIENT: MARK TAKEN / SKIPPED
        // =====================================================

        public async Task LogPatientMedicationAsync(
            Guid patientUserId,
            Guid medicationId,
            bool taken,
            string? notes
        )
        {
            var patient =
                await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId ==
                                patientUserId &&
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

            var medication =
                await _context.Medications
                    .FirstOrDefaultAsync(
                        m =>
                            m.Id ==
                                medicationId &&
                            m.PatientId ==
                                patient.Id &&
                            m.IsActive
                    );

            if (medication == null)
            {
                throw new KeyNotFoundException(
                    "Active medication not found."
                );
            }

            var log =
                new MedicationLog
                {
                    Id =
                        Guid.NewGuid(),

                    MedicationId =
                        medication.Id,

                    Taken =
                        taken,

                    Notes =
                        string.IsNullOrWhiteSpace(
                            notes
                        )
                            ? null
                            : notes.Trim(),

                    TakenAt =
                        DateTime.UtcNow
                };

            _context
                .MedicationLogs
                .Add(
                    log
                );

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                taken
                    ? "MedicationTaken"
                    : "MedicationSkipped",
                patientUserId,
                $"Medication {medication.Id} adherence logged.",
                patient.ClinicId
            );
        }

        // =====================================================
        // STAFF IDENTITY
        // =====================================================

        private async Task<Guid>
            GetStaffClinicIdAsync(
                Guid userId
            )
        {
            var user =
                await _context.Users
                    .Include(u => u.Admin)
                    .Include(u => u.Nurse)
                    .FirstOrDefaultAsync(
                        u =>
                            u.Id ==
                            userId
                    );

            if (
                user == null ||
                !user.IsActive
            )
            {
                throw new UnauthorizedAccessException(
                    "Active staff account required."
                );
            }

            if (
                user.Role ==
                    RoleNames.ClinicAdmin &&
                user.Admin?.ClinicId !=
                    null
            )
            {
                return user.Admin
                    .ClinicId.Value;
            }

            if (
                user.Role ==
                    RoleNames.Nurse &&
                user.Nurse != null
            )
            {
                return user.Nurse
                    .ClinicId;
            }

            throw new UnauthorizedAccessException(
                "Clinic staff privileges are required."
            );
        }
    }
}