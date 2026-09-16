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
        private readonly PhilaLinkDbContext _context;

        public MedicationSupplyController(
            PhilaLinkDbContext context
        )
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMySupply()
        {
            var userId =
                GetCurrentUserId();

            var patient =
                await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId == userId &&
                            p.User.Role ==
                                RoleNames.Patient &&
                            p.User.IsActive
                    );

            if (patient == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Active patient profile not found."
                    }
                );
            }

            var medications =
                await _context.Medications
                    .Include(
                        m => m.Schedules
                    )
                    .Where(
                        m =>
                            m.PatientId ==
                                patient.Id &&
                            m.IsActive
                    )
                    .OrderBy(
                        m => m.Name
                    )
                    .ToListAsync();

            var completedCollections =
                await _context
                    .MedicationCollections
                    .Include(
                        c => c.Items
                    )
                    .Where(
                        c =>
                            c.PatientId ==
                                patient.Id &&
                            c.Status ==
                                MedicationCollectionStatuses
                                    .Collected &&
                            c.CollectedAt != null
                    )
                    .OrderByDescending(
                        c => c.CollectedAt
                    )
                    .ToListAsync();

            var now =
                DateTime.UtcNow;

            var result =
                medications
                    .Select(
                        medication =>
                            BuildSupplyDto(
                                medication,
                                completedCollections,
                                now
                            )
                    )
                    .ToList();

            return Ok(result);
        }

        private static MedicationSupplyDto
            BuildSupplyDto(
                Medication medication,
                IReadOnlyList<
                    MedicationCollection
                > completedCollections,
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

            var latestCollection =
                completedCollections
                    .FirstOrDefault(
                        collection =>
                            collection.Items.Any(
                                item =>
                                    item.MedicationId ==
                                    medication.Id
                            )
                    );

            if (
                latestCollection == null ||
                latestCollection.CollectedAt ==
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
                latestCollection.Items
                    .Where(
                        item =>
                            item.MedicationId ==
                            medication.Id
                    )
                    .Sum(
                        item =>
                            item.Quantity
                    );

            if (
                medication.UnitsPerDose ==
                    null ||
                medication.UnitsPerDose <= 0
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
                        latestCollection
                            .CollectedAt,

                    CalculationStatus =
                        "MissingUnitsPerDose"
                };
            }

            if (
                activeSchedules.Count == 0
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
                        latestCollection
                            .CollectedAt,

                    CalculationStatus =
                        "MissingSchedule"
                };
            }

            var collectedAt =
                latestCollection
                    .CollectedAt.Value;

            var scheduledDosesUsed =
                CountScheduledDoses(
                    collectedAt,
                    now,
                    activeSchedules
                );

            var estimatedUnitsUsed =
                scheduledDosesUsed *
                medication
                    .UnitsPerDose.Value;

            var estimatedRemaining =
                Math.Max(
                    0m,
                    dispensedQuantity -
                    estimatedUnitsUsed
                );

            var unitsPerDay =
                activeSchedules.Count *
                medication
                    .UnitsPerDose.Value;

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
                schedules.Count == 0 ||
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
                    var schedule in schedules
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
                    date.AddDays(1);
            }

            return count;
        }

        private Guid GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
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
                throw new UnauthorizedAccessException();
            }

            return userId;
        }
    }
}