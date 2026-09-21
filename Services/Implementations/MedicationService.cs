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

        /*
         * South Africa uses UTC+2 throughout the year
         * and does not currently use daylight saving time.
         *
         * Medication daily-dose limits therefore use the
         * patient's South African calendar day instead of
         * the raw UTC calendar day.
         */
        private static readonly TimeSpan
            SouthAfricaUtcOffset =
                TimeSpan.FromHours(2);

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
             * Schedules and adherence logs are returned so the
             * patient UI can display today's doses, history and
             * adherence information.
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
            var nowUtc =
                DateTime.UtcNow;

            // -------------------------------------------------
            // Confirm the authenticated user owns an active
            // patient profile.
            // -------------------------------------------------

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
                    "Active patient profile not found."
                );
            }

            // -------------------------------------------------
            // Get the medication and its number of active
            // schedules.
            //
            // One active schedule represents one planned dose
            // per day.
            // -------------------------------------------------

            var medication =
                await _context.Medications
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.Id ==
                                medicationId &&
                            item.PatientId ==
                                patient.Id
                    )
                    .Select(
                        item =>
                            new MedicationLoggingAccess
                            {
                                Id =
                                    item.Id,

                                IsActive =
                                    item.IsActive,

                                StartDate =
                                    item.StartDate,

                                EndDate =
                                    item.EndDate,

                                ActiveScheduleCount =
                                    item.Schedules.Count(
                                        schedule =>
                                            schedule.IsActive
                                    )
                            }
                    )
                    .FirstOrDefaultAsync();

            if (medication == null)
            {
                throw new KeyNotFoundException(
                    "Medication not found."
                );
            }

            // -------------------------------------------------
            // Medication must currently be active.
            // -------------------------------------------------

            if (
                !medication.IsActive
            )
            {
                throw new InvalidOperationException(
                    "This medication is no longer active."
                );
            }

            if (
                medication.StartDate >
                nowUtc
            )
            {
                throw new InvalidOperationException(
                    "This medication has not started yet."
                );
            }

            if (
                medication.EndDate !=
                    null &&
                medication.EndDate.Value <
                    nowUtc
            )
            {
                throw new InvalidOperationException(
                    "This medication has ended and can no longer be logged."
                );
            }

            // -------------------------------------------------
            // Medication adherence requires at least one active
            // schedule. Without a schedule we cannot determine
            // how many doses are expected.
            // -------------------------------------------------

            if (
                medication.ActiveScheduleCount <=
                0
            )
            {
                throw new InvalidOperationException(
                    "This medication does not have an active dosing schedule."
                );
            }

            // -------------------------------------------------
            // DAILY TAKEN-DOSE LIMIT
            //
            // Example:
            //
            // 08:00 schedule
            // 20:00 schedule
            //
            // ActiveScheduleCount = 2
            //
            // The patient may therefore record at most two
            // Taken=true logs during the South African
            // calendar day.
            //
            // Skipped logs do not reduce medication supply and
            // are not included in the taken-dose limit.
            // -------------------------------------------------

            if (taken)
            {
                var (
                    dayStartUtc,
                    dayEndUtc
                ) =
                    GetSouthAfricaDayBoundsUtc(
                        nowUtc
                    );

                var dosesTakenToday =
                    await _context
                        .MedicationLogs
                        .AsNoTracking()
                        .CountAsync(
                            log =>
                                log.MedicationId ==
                                    medication.Id &&
                                log.Taken &&
                                log.TakenAt >=
                                    dayStartUtc &&
                                log.TakenAt <
                                    dayEndUtc
                        );

                if (
                    dosesTakenToday >=
                    medication
                        .ActiveScheduleCount
                )
                {
                    var doseWord =
                        medication
                            .ActiveScheduleCount ==
                        1
                            ? "dose"
                            : "doses";

                    throw new InvalidOperationException(
                        $"You have already recorded all {medication.ActiveScheduleCount} scheduled {doseWord} for this medication today."
                    );
                }
            }

            // -------------------------------------------------
            // SAVE ADHERENCE LOG
            // -------------------------------------------------

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
                        nowUtc
                };

            _context.MedicationLogs.Add(
                log
            );

            await _context.SaveChangesAsync();

            // -------------------------------------------------
            // AUDIT
            // -------------------------------------------------

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
        // SOUTH AFRICAN CALENDAR DAY
        // =====================================================

        private static (
            DateTime StartUtc,
            DateTime EndUtc
        )
            GetSouthAfricaDayBoundsUtc(
                DateTime utcNow
            )
        {
            /*
             * Convert the current UTC instant to SAST.
             *
             * Using DateTimeOffset here avoids depending on the
             * operating system's time-zone database.
             */
            var localNow =
                new DateTimeOffset(
                    DateTime.SpecifyKind(
                        utcNow,
                        DateTimeKind.Utc
                    )
                )
                .ToOffset(
                    SouthAfricaUtcOffset
                );

            var localDayStart =
                new DateTimeOffset(
                    localNow.Year,
                    localNow.Month,
                    localNow.Day,
                    0,
                    0,
                    0,
                    SouthAfricaUtcOffset
                );

            var localDayEnd =
                localDayStart.AddDays(
                    1
                );

            return (
                localDayStart.UtcDateTime,
                localDayEnd.UtcDateTime
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

        // =====================================================
        // INTERNAL QUERY MODELS
        // =====================================================

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

        private sealed class MedicationLoggingAccess
        {
            public Guid Id
            {
                get;
                init;
            }

            public bool IsActive
            {
                get;
                init;
            }

            public DateTime StartDate
            {
                get;
                init;
            }

            public DateTime? EndDate
            {
                get;
                init;
            }

            public int ActiveScheduleCount
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