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
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.Id ==
                                dto.PatientId &&
                            item.User.IsActive
                    )
                    .Select(
                        item =>
                            new PatientMedicationAccess
                            {
                                Id =
                                    item.Id,

                                ClinicId =
                                    item.ClinicId
                            }
                    )
                    .FirstOrDefaultAsync();

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

            await _context.SaveChangesAsync();

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
            var patientId =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        patient =>
                            patient.UserId ==
                                patientUserId &&
                            patient.User.Role ==
                                RoleNames.Patient &&
                            patient.User.IsActive
                    )
                    .Select(
                        patient =>
                            (Guid?)patient.Id
                    )
                    .FirstOrDefaultAsync();

            if (patientId == null)
            {
                throw new KeyNotFoundException(
                    "Active patient profile not found."
                );
            }

            /*
             * Preserve the existing API response: schedules and
             * adherence logs are still returned. Read tracking
             * is unnecessary for this endpoint.
             */
            return await _context.Medications
                .AsNoTrackingWithIdentityResolution()
                .AsSplitQuery()
                .Include(
                    medication =>
                        medication.Schedules
                )
                .Include(
                    medication =>
                        medication.Logs
                )
                .Where(
                    medication =>
                        medication.PatientId ==
                            patientId.Value
                )
                .OrderByDescending(
                    medication =>
                        medication.IsActive
                )
                .ThenBy(
                    medication =>
                        medication.Name
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
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.Id ==
                                patientId
                    )
                    .Select(
                        item =>
                            new PatientMedicationAccess
                            {
                                Id =
                                    item.Id,

                                ClinicId =
                                    item.ClinicId
                            }
                    )
                    .FirstOrDefaultAsync();

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

            return await _context.Medications
                .AsNoTrackingWithIdentityResolution()
                .AsSplitQuery()
                .Include(
                    medication =>
                        medication.Schedules
                )
                .Include(
                    medication =>
                        medication.Logs
                )
                .Where(
                    medication =>
                        medication.PatientId ==
                            patientId
                )
                .OrderBy(
                    medication =>
                        medication.Name
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
                await GetMedicationClinicAccessAsync(
                    medicationId
                );

            if (medication == null)
            {
                throw new KeyNotFoundException(
                    "Medication not found."
                );
            }

            if (
                medication.ClinicId !=
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
                await _context.MedicationSchedules
                    .AsNoTracking()
                    .AnyAsync(
                        schedule =>
                            schedule.MedicationId ==
                                medicationId &&
                            schedule.TimeOfDay ==
                                scheduleTime &&
                            schedule.IsActive
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

            _context.MedicationSchedules.Add(
                schedule
            );

            await _context.SaveChangesAsync();

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
                await GetMedicationClinicAccessAsync(
                    medicationId
                );

            if (medication == null)
            {
                throw new KeyNotFoundException(
                    "Medication not found."
                );
            }

            if (
                medication.ClinicId !=
                clinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "Medication belongs to a patient outside your clinic."
                );
            }

            return await _context.MedicationLogs
                .AsNoTracking()
                .Where(
                    log =>
                        log.MedicationId ==
                            medicationId
                )
                .OrderByDescending(
                    log =>
                        log.TakenAt
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
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.UserId ==
                                patientUserId &&
                            item.User.Role ==
                                RoleNames.Patient &&
                            item.User.IsActive
                    )
                    .Select(
                        item =>
                            new PatientMedicationAccess
                            {
                                Id =
                                    item.Id,

                                ClinicId =
                                    item.ClinicId
                            }
                    )
                    .FirstOrDefaultAsync();

            if (patient == null)
            {
                throw new UnauthorizedAccessException(
                    "Active Patient profile not found."
                );
            }

            var activeMedicationId =
                await _context.Medications
                    .AsNoTracking()
                    .Where(
                        medication =>
                            medication.Id ==
                                medicationId &&
                            medication.PatientId ==
                                patient.Id &&
                            medication.IsActive
                    )
                    .Select(
                        medication =>
                            (Guid?)medication.Id
                    )
                    .FirstOrDefaultAsync();

            if (activeMedicationId == null)
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
                        activeMedicationId.Value,

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

            _context.MedicationLogs.Add(
                log
            );

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                taken
                    ? "MedicationTaken"
                    : "MedicationSkipped",
                patientUserId,
                $"Medication {activeMedicationId.Value} adherence logged.",
                patient.ClinicId
            );
        }

        // =====================================================
        // READ HELPERS
        // =====================================================

        private async Task<MedicationClinicAccess?>
            GetMedicationClinicAccessAsync(
                Guid medicationId
            )
        {
            return await _context.Medications
                .AsNoTracking()
                .Where(
                    medication =>
                        medication.Id ==
                            medicationId
                )
                .Select(
                    medication =>
                        new MedicationClinicAccess
                        {
                            Id =
                                medication.Id,

                            ClinicId =
                                medication
                                    .Patient
                                    .ClinicId
                        }
                )
                .FirstOrDefaultAsync();
        }

        // =====================================================
        // STAFF IDENTITY
        // =====================================================

        private async Task<Guid>
            GetStaffClinicIdAsync(
                Guid userId
            )
        {
            var staff =
                await _context.Users
                    .AsNoTracking()
                    .Where(
                        user =>
                            user.Id ==
                                userId &&
                            user.IsActive
                    )
                    .Select(
                        user =>
                            new StaffClinicAccess
                            {
                                Role =
                                    user.Role,

                                AdminClinicId =
                                    user.Admin == null
                                        ? null
                                        : user
                                            .Admin
                                            .ClinicId,

                                NurseClinicId =
                                    user.Nurse == null
                                        ? null
                                        : (Guid?)user
                                            .Nurse
                                            .ClinicId
                            }
                    )
                    .FirstOrDefaultAsync();

            if (staff == null)
            {
                throw new UnauthorizedAccessException(
                    "Active staff account required."
                );
            }

            if (
                staff.Role ==
                    RoleNames.ClinicAdmin &&
                staff.AdminClinicId !=
                    null
            )
            {
                return staff.AdminClinicId.Value;
            }

            if (
                staff.Role ==
                    RoleNames.Nurse &&
                staff.NurseClinicId !=
                    null
            )
            {
                return staff.NurseClinicId.Value;
            }

            throw new UnauthorizedAccessException(
                "Clinic staff privileges are required."
            );
        }

        private sealed class PatientMedicationAccess
        {
            public Guid Id
            {
                get;
                init;
            }

            public Guid? ClinicId
            {
                get;
                init;
            }
        }

        private sealed class MedicationClinicAccess
        {
            public Guid Id
            {
                get;
                init;
            }

            public Guid? ClinicId
            {
                get;
                init;
            }
        }

        private sealed class StaffClinicAccess
        {
            public string Role
            {
                get;
                init;
            } = string.Empty;

            public Guid? AdminClinicId
            {
                get;
                init;
            }

            public Guid? NurseClinicId
            {
                get;
                init;
            }
        }
    }
}
