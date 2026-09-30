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
            _context =
                context;

            _audit =
                audit;
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

            if (
                dto.ClinicStockId ==
                    Guid.Empty
            )
            {
                throw new InvalidOperationException(
                    "Select a medication from the clinic inventory."
                );
            }

            var stock =
                await _context
                    .ClinicStocks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                dto.ClinicStockId &&
                            item.ClinicId ==
                                clinicId &&
                            item.IsActive
                    );

            if (
                stock ==
                null
            )
            {
                throw new InvalidOperationException(
                    "The selected medication is not available in your clinic inventory."
                );
            }

            if (
                stock.QuantityOnHand <=
                0
            )
            {
                throw new InvalidOperationException(
                    "The selected medication is currently out of stock."
                );
            }

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

            if (
                patient ==
                null
            )
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
                dto.UnitsPerDose !=
                    null &&
                dto.UnitsPerDose <=
                    0
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

                    /*
                     * Medication identity comes directly from
                     * ClinicStock so collection scheduling can
                     * reliably find the same inventory item.
                     */
                    Name =
                        stock
                            .MedicationName
                            .Trim(),

                    Dosage =
                        stock
                            .Strength
                            .Trim(),

                    Form =
                        stock
                            .Form
                            .Trim(),

                    Instructions =
                        dto.Instructions
                            .Trim(),

                    UnitsPerDose =
                        dto.UnitsPerDose,

                    PrescribedBy =
                        string.IsNullOrWhiteSpace(
                            dto.PrescribedBy
                        )
                            ? null
                            : dto
                                .PrescribedBy
                                .Trim(),

                    ConditionName =
                        string.IsNullOrWhiteSpace(
                            dto.ConditionName
                        )
                            ? null
                            : dto
                                .ConditionName
                                .Trim(),

                    StartDate =
                        dto.StartDate ==
                            default
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
                $"Medication {medication.Id} created for patient {patient.Id} from clinic stock {stock.Id}.",
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

            if (
                patientId ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Active patient profile not found."
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

            if (
                patient ==
                null
            )
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

            if (
                medication ==
                null
            )
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

            if (
                duplicate
            )
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
                await GetMedicationClinicAccessAsync(
                    medicationId
                );

            if (
                medication ==
                null
            )
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

            if (
                patient ==
                null
            )
            {
                throw new UnauthorizedAccessException(
                    "Active patient profile not found."
                );
            }

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

                                UnitsPerDose =
                                    item.UnitsPerDose,

                                ActiveScheduleCount =
                                    item.Schedules.Count(
                                        schedule =>
                                            schedule.IsActive
                                    )
                            }
                    )
                    .FirstOrDefaultAsync();

            if (
                medication ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Medication not found."
                );
            }

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

            if (
                medication.ActiveScheduleCount <=
                0
            )
            {
                throw new InvalidOperationException(
                    "This medication does not have an active dosing schedule."
                );
            }

            if (
                taken
            )
            {
                if (
                    medication.UnitsPerDose ==
                        null ||
                    medication.UnitsPerDose <=
                        0
                )
                {
                    throw new InvalidOperationException(
                        "The dose amount for this medication has not been configured. Contact your clinic before recording a taken dose."
                    );
                }

                var latestCollection =
                    await _context
                        .MedicationCollections
                        .AsNoTracking()
                        .Where(
                            collection =>
                                collection.PatientId ==
                                    patient.Id &&
                                collection.Status ==
                                    MedicationCollectionStatuses
                                        .Collected &&
                                collection.CollectedAt !=
                                    null &&
                                collection.CollectedAt <=
                                    nowUtc &&
                                collection.Items.Any(
                                    item =>
                                        item.MedicationId ==
                                            medication.Id
                                )
                        )
                        .OrderByDescending(
                            collection =>
                                collection.CollectedAt
                        )
                        .Select(
                            collection =>
                                new MedicationDispenseAccess
                                {
                                    CollectionId =
                                        collection.Id,

                                    CollectedAt =
                                        collection
                                            .CollectedAt!
                                            .Value,

                                    DispensedQuantity =
                                        collection.Items
                                            .Where(
                                                item =>
                                                    item.MedicationId ==
                                                        medication.Id
                                            )
                                            .Sum(
                                                item =>
                                                    item.Quantity
                                            )
                                }
                        )
                        .FirstOrDefaultAsync();

                if (
                    latestCollection ==
                    null
                )
                {
                    throw new InvalidOperationException(
                        "You cannot mark this medication as taken because no completed medication collection has been recorded."
                    );
                }

                var takenSinceCollection =
                    await _context
                        .MedicationLogs
                        .AsNoTracking()
                        .CountAsync(
                            log =>
                                log.MedicationId ==
                                    medication.Id &&
                                log.Taken &&
                                log.TakenAt >=
                                    latestCollection
                                        .CollectedAt &&
                                log.TakenAt <=
                                    nowUtc
                        );

                var unitsUsed =
                    takenSinceCollection *
                    medication
                        .UnitsPerDose
                        .Value;

                var unitsRemaining =
                    latestCollection
                        .DispensedQuantity -
                    unitsUsed;

                if (
                    unitsRemaining <
                    medication
                        .UnitsPerDose
                        .Value
                )
                {
                    throw new InvalidOperationException(
                        "You cannot mark another dose as taken because your recorded medication supply has been depleted."
                    );
                }

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
                                    user.Admin ==
                                        null
                                        ? null
                                        : user
                                            .Admin
                                            .ClinicId,

                                NurseClinicId =
                                    user.Nurse ==
                                        null
                                        ? null
                                        : (Guid?)user
                                            .Nurse
                                            .ClinicId
                            }
                    )
                    .FirstOrDefaultAsync();

            if (
                staff ==
                null
            )
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
                return staff
                    .AdminClinicId
                    .Value;
            }

            if (
                staff.Role ==
                    RoleNames.Nurse &&
                staff.NurseClinicId !=
                    null
            )
            {
                return staff
                    .NurseClinicId
                    .Value;
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

            public decimal? UnitsPerDose
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

        private sealed class MedicationDispenseAccess
        {
            public Guid CollectionId
            {
                get;
                init;
            }

            public DateTime CollectedAt
            {
                get;
                init;
            }

            public int DispensedQuantity
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
            } =
                string.Empty;

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
