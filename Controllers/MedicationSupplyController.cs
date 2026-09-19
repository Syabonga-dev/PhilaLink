using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/medications/me/supply")]
    [Authorize(Roles = RoleNames.Patient)]
    public class MedicationSupplyController :
        ControllerBase
    {
        private readonly PhilaLinkDbContext
            _context;

        public MedicationSupplyController(
            PhilaLinkDbContext context
        )
        {
            _context =
                context;
        }

        [HttpGet]
        public async Task<IActionResult>
            GetMySupply(
                CancellationToken
                    cancellationToken
            )
        {
            var userId =
                GetCurrentUserId();

            var patientId =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        patient =>
                            patient.UserId ==
                                userId &&
                            patient.User.Role ==
                                RoleNames.Patient &&
                            patient.User.IsActive
                    )
                    .Select(
                        patient =>
                            (Guid?)patient.Id
                    )
                    .FirstOrDefaultAsync(
                        cancellationToken
                    );

            if (
                patientId ==
                    null
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            "Active patient profile not found."
                    }
                );
            }

            var now =
                DateTime.UtcNow;

            var medications =
                await _context.Medications
                    .AsNoTracking()
                    .Include(
                        medication =>
                            medication.Schedules
                                .Where(
                                    schedule =>
                                        schedule.IsActive
                                )
                    )
                    .Where(
                        medication =>
                            medication.PatientId ==
                                patientId.Value &&
                            medication.IsActive &&
                            medication.StartDate <=
                                now &&
                            (
                                medication.EndDate ==
                                    null ||
                                medication.EndDate >=
                                    now
                            )
                    )
                    .OrderBy(
                        medication =>
                            medication.Name
                    )
                    .ToListAsync(
                        cancellationToken
                    );

            if (
                medications.Count ==
                    0
            )
            {
                return Ok(
                    Array.Empty<
                        MedicationSupplyDto
                    >()
                );
            }

            var medicationIds =
                medications
                    .Select(
                        medication =>
                            medication.Id
                    )
                    .ToList();

            // =================================================
            // LATEST COMPLETED COLLECTION
            // =================================================

            var collectedItems =
                await _context
                    .MedicationCollectionItems
                    .AsNoTracking()
                    .Where(
                        item =>
                            medicationIds.Contains(
                                item.MedicationId
                            ) &&
                            item
                                .MedicationCollection
                                .PatientId ==
                                    patientId.Value &&
                            item
                                .MedicationCollection
                                .Status ==
                                    MedicationCollectionStatuses
                                        .Collected &&
                            item
                                .MedicationCollection
                                .CollectedAt !=
                                    null
                    )
                    .Select(
                        item =>
                            new CollectedItemRow
                            {
                                CollectionId =
                                    item
                                        .MedicationCollectionId,

                                MedicationId =
                                    item.MedicationId,

                                Quantity =
                                    item.Quantity,

                                CollectedAt =
                                    item
                                        .MedicationCollection
                                        .CollectedAt!
                                        .Value
                            }
                    )
                    .ToListAsync(
                        cancellationToken
                    );

            var latestDispenseByMedication =
                collectedItems
                    .GroupBy(
                        item =>
                            item.MedicationId
                    )
                    .ToDictionary(
                        medicationGroup =>
                            medicationGroup.Key,

                        medicationGroup =>
                        {
                            var latestCollection =
                                medicationGroup
                                    .GroupBy(
                                        item =>
                                            new
                                            {
                                                item.CollectionId,
                                                item.CollectedAt
                                            }
                                    )
                                    .OrderByDescending(
                                        collectionGroup =>
                                            collectionGroup
                                                .Key
                                                .CollectedAt
                                    )
                                    .First();

                            return new LatestDispense
                            {
                                CollectedAt =
                                    latestCollection
                                        .Key
                                        .CollectedAt,

                                Quantity =
                                    latestCollection
                                        .Sum(
                                            item =>
                                                item.Quantity
                                        )
                            };
                        }
                    );

            // =================================================
            // ACTUAL TAKEN DOSES
            // =================================================

            /*
             * Supply now decreases when the patient explicitly
             * marks a dose as Taken.
             *
             * Skipped entries do not consume supply.
             */
            var takenLogs =
                await _context
                    .MedicationLogs
                    .AsNoTracking()
                    .Where(
                        log =>
                            medicationIds.Contains(
                                log.MedicationId
                            ) &&
                            log.Taken &&
                            log.TakenAt <=
                                now
                    )
                    .Select(
                        log =>
                            new TakenLogRow
                            {
                                MedicationId =
                                    log.MedicationId,

                                TakenAt =
                                    log.TakenAt
                            }
                    )
                    .ToListAsync(
                        cancellationToken
                    );

            var result =
                medications
                    .Select(
                        medication =>
                        {
                            latestDispenseByMedication
                                .TryGetValue(
                                    medication.Id,
                                    out var latestDispense
                                );

                            return BuildSupplyDto(
                                medication,
                                latestDispense,
                                takenLogs,
                                now
                            );
                        }
                    )
                    .ToList();

            return Ok(
                result
            );
        }

        // =====================================================
        // CALCULATION
        // =====================================================

        private static MedicationSupplyDto
            BuildSupplyDto(
                Medication medication,
                LatestDispense?
                    latestDispense,
                IReadOnlyCollection<TakenLogRow>
                    takenLogs,
                DateTime now
            )
        {
            var activeSchedules =
                medication.Schedules
                    .Where(
                        schedule =>
                            schedule.IsActive
                    )
                    .OrderBy(
                        schedule =>
                            schedule.TimeOfDay
                    )
                    .ToList();

            if (
                latestDispense ==
                    null
            )
            {
                return new MedicationSupplyDto
                {
                    MedicationId =
                        medication.Id,

                    Name =
                        medication.Name,

                    Dosage =
                        medication.Dosage,

                    Form =
                        medication.Form,

                    UnitsPerDose =
                        medication.UnitsPerDose,

                    DosesPerDay =
                        activeSchedules.Count,

                    DispensedQuantity =
                        null,

                    EstimatedRemainingQuantity =
                        null,

                    DaysRemaining =
                        null,

                    LastCollectedAt =
                        null,

                    CalculationStatus =
                        "NoCompletedCollection"
                };
            }

            var dispensedQuantity =
                latestDispense.Quantity;

            if (
                medication.UnitsPerDose ==
                    null ||
                medication.UnitsPerDose <=
                    0
            )
            {
                return new MedicationSupplyDto
                {
                    MedicationId =
                        medication.Id,

                    Name =
                        medication.Name,

                    Dosage =
                        medication.Dosage,

                    Form =
                        medication.Form,

                    UnitsPerDose =
                        medication.UnitsPerDose,

                    DosesPerDay =
                        activeSchedules.Count,

                    DispensedQuantity =
                        dispensedQuantity,

                    EstimatedRemainingQuantity =
                        null,

                    DaysRemaining =
                        null,

                    LastCollectedAt =
                        latestDispense
                            .CollectedAt,

                    CalculationStatus =
                        "MissingUnitsPerDose"
                };
            }

            if (
                activeSchedules.Count ==
                    0
            )
            {
                return new MedicationSupplyDto
                {
                    MedicationId =
                        medication.Id,

                    Name =
                        medication.Name,

                    Dosage =
                        medication.Dosage,

                    Form =
                        medication.Form,

                    UnitsPerDose =
                        medication.UnitsPerDose,

                    DosesPerDay =
                        0,

                    DispensedQuantity =
                        dispensedQuantity,

                    EstimatedRemainingQuantity =
                        null,

                    DaysRemaining =
                        null,

                    LastCollectedAt =
                        latestDispense
                            .CollectedAt,

                    CalculationStatus =
                        "MissingSchedule"
                };
            }

            var recordedTakenDoses =
                takenLogs.Count(
                    log =>
                        log.MedicationId ==
                            medication.Id &&
                        log.TakenAt >=
                            latestDispense
                                .CollectedAt &&
                        log.TakenAt <=
                            now
                );

            var estimatedUnitsUsed =
                recordedTakenDoses *
                medication
                    .UnitsPerDose
                    .Value;

            var estimatedRemaining =
                Math.Max(
                    0m,
                    dispensedQuantity -
                    estimatedUnitsUsed
                );

            var unitsPerDay =
                activeSchedules.Count *
                medication
                    .UnitsPerDose
                    .Value;

            int? daysRemaining =
                null;

            if (
                unitsPerDay >
                    0
            )
            {
                daysRemaining =
                    (int)Math.Floor(
                        estimatedRemaining /
                        unitsPerDay
                    );
            }

            return new MedicationSupplyDto
            {
                MedicationId =
                    medication.Id,

                Name =
                    medication.Name,

                Dosage =
                    medication.Dosage,

                Form =
                    medication.Form,

                UnitsPerDose =
                    medication.UnitsPerDose,

                DosesPerDay =
                    activeSchedules.Count,

                DispensedQuantity =
                    dispensedQuantity,

                EstimatedRemainingQuantity =
                    estimatedRemaining,

                DaysRemaining =
                    daysRemaining,

                LastCollectedAt =
                    latestDispense
                        .CollectedAt,

                CalculationStatus =
                    "Available"
            };
        }

        // =====================================================
        // CURRENT USER
        // =====================================================

        private Guid
            GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes
                        .NameIdentifier
                );

            if (
                string.IsNullOrWhiteSpace(
                    value
                ) ||
                !Guid.TryParse(
                    value,
                    out var userId
                )
            )
            {
                throw new
                    UnauthorizedAccessException();
            }

            return userId;
        }

        // =====================================================
        // INTERNAL MODELS
        // =====================================================

        private sealed class
            CollectedItemRow
        {
            public Guid CollectionId
            {
                get;
                init;
            }

            public Guid MedicationId
            {
                get;
                init;
            }

            public int Quantity
            {
                get;
                init;
            }

            public DateTime CollectedAt
            {
                get;
                init;
            }
        }

        private sealed class
            LatestDispense
        {
            public int Quantity
            {
                get;
                init;
            }

            public DateTime CollectedAt
            {
                get;
                init;
            }
        }

        private sealed class
            TakenLogRow
        {
            public Guid MedicationId
            {
                get;
                init;
            }

            public DateTime TakenAt
            {
                get;
                init;
            }
        }
    }
}
