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
    public class MedicationSupplyController : ControllerBase
    {
        private readonly PhilaLinkDbContext _context;

        public MedicationSupplyController(
            PhilaLinkDbContext context
        )
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMySupply(
            CancellationToken cancellationToken
        )
        {
            var userId =
                GetCurrentUserId();

            /*
             * Only retrieve the Patient ID.
             *
             * The old implementation loaded the complete
             * Patient + User entity even though this endpoint
             * only needs Patient.Id.
             */
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

            if (patientId == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Active patient profile not found."
                    }
                );
            }

            /*
             * Only active medications are required.
             *
             * Only active schedules are loaded.
             * Tracking is unnecessary for this read-only request.
             */
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
                            medication.IsActive
                    )
                    .OrderBy(
                        medication =>
                            medication.Name
                    )
                    .ToListAsync(
                        cancellationToken
                    );

            if (medications.Count == 0)
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

            /*
             * The old implementation loaded every completed
             * MedicationCollection entity plus its Items.
             *
             * We only need:
             * - collection ID
             * - medication ID
             * - quantity
             * - collected timestamp
             */
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

            /*
             * Find only the latest completed collection
             * for each medication.
             */
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

            var now =
                DateTime.UtcNow;

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
                                now
                            );
                        }
                    )
                    .ToList();

            return Ok(result);
        }

        private static MedicationSupplyDto
            BuildSupplyDto(
                Medication medication,
                LatestDispense? latestDispense,
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

            if (latestDispense == null)
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

            var collectedAt =
                latestDispense
                    .CollectedAt;

            var scheduledDosesUsed =
                CountScheduledDoses(
                    collectedAt,
                    now,
                    activeSchedules
                );

            var estimatedUnitsUsed =
                scheduledDosesUsed *
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

            if (unitsPerDay > 0)
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
                    collectedAt,

                CalculationStatus =
                    "Available"
            };
        }

        private static int CountScheduledDoses(
            DateTime from,
            DateTime to,
            IReadOnlyList<
                MedicationSchedule
            > schedules
        )
        {
            if (
                schedules.Count ==
                    0 ||
                to <= from
            )
            {
                return 0;
            }

            var count =
                0;

            var date =
                from.Date;

            var lastDate =
                to.Date;

            while (
                date <= lastDate
            )
            {
                foreach (
                    var schedule
                    in schedules
                )
                {
                    var occurrence =
                        date +
                        schedule.TimeOfDay;

                    if (
                        occurrence >
                            from &&
                        occurrence <=
                            to
                    )
                    {
                        count++;
                    }
                }

                date =
                    date.AddDays(
                        1
                    );
            }

            return count;
        }

        private Guid GetCurrentUserId()
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
    }
}
