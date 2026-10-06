using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Controllers;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Tests.Support;
using System.Security.Claims;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class MedicationSupplyWorkflowTests
    {
        [Fact]
        public async Task
            TakenDosesReduceSupplyWhileSkippedAndOldLogsDoNot()
        {
            await using var db =
                TestDb.Create();

            var now =
                DateTime.UtcNow;

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "1301"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinic,
                    "PHL-TEST-1301"
                );

            var stock =
                Stock(
                    clinic
                );

            var medication =
                Medication(
                    patient,
                    unitsPerDose:
                        2
                );

            var morningSchedule =
                new MedicationSchedule
                {
                    Id =
                        Guid.NewGuid(),

                    MedicationId =
                        medication.Id,

                    Medication =
                        medication,

                    TimeOfDay =
                        new TimeSpan(
                            8,
                            0,
                            0
                        ),

                    IsActive =
                        true
                };

            var eveningSchedule =
                new MedicationSchedule
                {
                    Id =
                        Guid.NewGuid(),

                    MedicationId =
                        medication.Id,

                    Medication =
                        medication,

                    TimeOfDay =
                        new TimeSpan(
                            20,
                            0,
                            0
                        ),

                    IsActive =
                        true
                };

            var collectedAt =
                now.AddDays(
                    -3
                );

            var collection =
                Collection(
                    patient,
                    clinic,
                    collectedAt
                );

            var collectionItem =
                CollectionItem(
                    collection,
                    medication,
                    stock,
                    quantity:
                        60
                );

            /*
             * Three taken doses after collection.
             *
             * 3 doses x 2 units = 6 units used.
             */
            var takenOne =
                Log(
                    medication,
                    collectedAt
                        .AddHours(
                            4
                        ),
                    taken:
                        true
                );

            var takenTwo =
                Log(
                    medication,
                    collectedAt
                        .AddHours(
                            16
                        ),
                    taken:
                        true
                );

            var takenThree =
                Log(
                    medication,
                    collectedAt
                        .AddDays(
                            1
                        )
                        .AddHours(
                            4
                        ),
                    taken:
                        true
                );

            /*
             * A skipped dose must not consume supply.
             */
            var skipped =
                Log(
                    medication,
                    collectedAt
                        .AddDays(
                            1
                        )
                        .AddHours(
                            16
                        ),
                    taken:
                        false
                );

            /*
             * Historical logs before the latest collection
             * must not consume the newly dispensed supply.
             */
            var beforeCollection =
                Log(
                    medication,
                    collectedAt
                        .AddDays(
                            -1
                        ),
                    taken:
                        true
                );

            db.AddRange(
                clinic,
                patientUser,
                patient,
                stock,
                medication,
                morningSchedule,
                eveningSchedule,
                collection,
                collectionItem,
                takenOne,
                takenTwo,
                takenThree,
                skipped,
                beforeCollection
            );

            await db.SaveChangesAsync();

            var controller =
                CreateController(
                    db,
                    patientUser.Id
                );

            var actionResult =
                await controller
                    .GetMySupply(
                        CancellationToken.None
                    );

            var ok =
                Assert.IsType<
                    OkObjectResult
                >(
                    actionResult
                );

            var rows =
                Assert.IsType<
                    List<
                        MedicationSupplyDto
                    >
                >(
                    ok.Value
                );

            var supply =
                Assert.Single(
                    rows
                );

            Assert.Equal(
                medication.Id,
                supply.MedicationId
            );

            Assert.Equal(
                2m,
                supply.UnitsPerDose
            );

            Assert.Equal(
                2,
                supply.DosesPerDay
            );

            Assert.Equal(
                60,
                supply.DispensedQuantity
            );

            /*
             * 60 dispensed
             * - 3 recorded taken doses x 2 units
             * = 54 units remaining.
             */
            Assert.Equal(
                54m,
                supply.EstimatedRemainingQuantity
            );

            /*
             * 2 schedules/day x 2 units/dose
             * = 4 units/day.
             *
             * floor(54 / 4) = 13 days.
             */
            Assert.Equal(
                13,
                supply.DaysRemaining
            );

            Assert.Equal(
                collectedAt,
                supply.LastCollectedAt
            );

            Assert.Equal(
                "Available",
                supply.CalculationStatus
            );
        }

        [Fact]
        public async Task
            LatestCollectionResetsSupplyCalculation()
        {
            await using var db =
                TestDb.Create();

            var now =
                DateTime.UtcNow;

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "1302"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinic,
                    "PHL-TEST-1302"
                );

            var stock =
                Stock(
                    clinic
                );

            var medication =
                Medication(
                    patient,
                    unitsPerDose:
                        1
                );

            var schedule =
                new MedicationSchedule
                {
                    Id =
                        Guid.NewGuid(),

                    MedicationId =
                        medication.Id,

                    Medication =
                        medication,

                    TimeOfDay =
                        new TimeSpan(
                            8,
                            0,
                            0
                        ),

                    IsActive =
                        true
                };

            var oldCollectedAt =
                now.AddDays(
                    -10
                );

            var latestCollectedAt =
                now.AddDays(
                    -2
                );

            var oldCollection =
                Collection(
                    patient,
                    clinic,
                    oldCollectedAt
                );

            var oldItem =
                CollectionItem(
                    oldCollection,
                    medication,
                    stock,
                    quantity:
                        30
                );

            var latestCollection =
                Collection(
                    patient,
                    clinic,
                    latestCollectedAt
                );

            var latestItem =
                CollectionItem(
                    latestCollection,
                    medication,
                    stock,
                    quantity:
                        40
                );

            /*
             * This dose belongs to the old collection period
             * and must not reduce the latest 40-unit dispense.
             */
            var oldTakenLog =
                Log(
                    medication,
                    oldCollectedAt
                        .AddDays(
                            1
                        ),
                    taken:
                        true
                );

            var latestTakenOne =
                Log(
                    medication,
                    latestCollectedAt
                        .AddHours(
                            4
                        ),
                    taken:
                        true
                );

            var latestTakenTwo =
                Log(
                    medication,
                    latestCollectedAt
                        .AddDays(
                            1
                        )
                        .AddHours(
                            4
                        ),
                    taken:
                        true
                );

            db.AddRange(
                clinic,
                patientUser,
                patient,
                stock,
                medication,
                schedule,
                oldCollection,
                oldItem,
                latestCollection,
                latestItem,
                oldTakenLog,
                latestTakenOne,
                latestTakenTwo
            );

            await db.SaveChangesAsync();

            var controller =
                CreateController(
                    db,
                    patientUser.Id
                );

            var actionResult =
                await controller
                    .GetMySupply(
                        CancellationToken.None
                    );

            var ok =
                Assert.IsType<
                    OkObjectResult
                >(
                    actionResult
                );

            var rows =
                Assert.IsType<
                    List<
                        MedicationSupplyDto
                    >
                >(
                    ok.Value
                );

            var supply =
                Assert.Single(
                    rows
                );

            Assert.Equal(
                40,
                supply.DispensedQuantity
            );

            /*
             * Only two taken doses occurred after the
             * latest collection.
             */
            Assert.Equal(
                38m,
                supply.EstimatedRemainingQuantity
            );

            Assert.Equal(
                38,
                supply.DaysRemaining
            );

            Assert.Equal(
                latestCollectedAt,
                supply.LastCollectedAt
            );

            Assert.Equal(
                "Available",
                supply.CalculationStatus
            );
        }

        [Fact]
        public async Task
            MedicationWithoutCompletedCollectionHasNoSupplyEstimate()
        {
            await using var db =
                TestDb.Create();

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "1303"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinic,
                    "PHL-TEST-1303"
                );

            var medication =
                Medication(
                    patient,
                    unitsPerDose:
                        1
                );

            var schedule =
                new MedicationSchedule
                {
                    Id =
                        Guid.NewGuid(),

                    MedicationId =
                        medication.Id,

                    Medication =
                        medication,

                    TimeOfDay =
                        new TimeSpan(
                            8,
                            0,
                            0
                        ),

                    IsActive =
                        true
                };

            db.AddRange(
                clinic,
                patientUser,
                patient,
                medication,
                schedule
            );

            await db.SaveChangesAsync();

            var controller =
                CreateController(
                    db,
                    patientUser.Id
                );

            var actionResult =
                await controller
                    .GetMySupply(
                        CancellationToken.None
                    );

            var ok =
                Assert.IsType<
                    OkObjectResult
                >(
                    actionResult
                );

            var rows =
                Assert.IsType<
                    List<
                        MedicationSupplyDto
                    >
                >(
                    ok.Value
                );

            var supply =
                Assert.Single(
                    rows
                );

            Assert.Null(
                supply.DispensedQuantity
            );

            Assert.Null(
                supply.EstimatedRemainingQuantity
            );

            Assert.Null(
                supply.DaysRemaining
            );

            Assert.Null(
                supply.LastCollectedAt
            );

            Assert.Equal(
                "NoCompletedCollection",
                supply.CalculationStatus
            );
        }

        private static MedicationSupplyController
            CreateController(
                PersonalProject.Data
                    .PhilaLinkDbContext db,
                Guid userId
            )
        {
            var identity =
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            userId.ToString()
                        ),

                        new Claim(
                            ClaimTypes.Role,
                            RoleNames.Patient
                        )
                    },
                    "Test"
                );

            var controller =
                new MedicationSupplyController(
                    db
                );

            controller.ControllerContext =
                new ControllerContext
                {
                    HttpContext =
                        new DefaultHttpContext
                        {
                            User =
                                new ClaimsPrincipal(
                                    identity
                                )
                        }
                };

            return controller;
        }

        private static ClinicStock
            Stock(
                Clinic clinic
            )
        {
            return new ClinicStock
            {
                Id =
                    Guid.NewGuid(),

                ClinicId =
                    clinic.Id,

                Clinic =
                    clinic,

                MedicationName =
                    "Metformin",

                Strength =
                    "500 mg",

                Form =
                    "Tablet",

                Unit =
                    "tablets",

                QuantityOnHand =
                    500,

                ReorderLevel =
                    50,

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow
            };
        }

        private static Medication
            Medication(
                Patient patient,
                decimal unitsPerDose
            )
        {
            return new Medication
            {
                Id =
                    Guid.NewGuid(),

                PatientId =
                    patient.Id,

                Patient =
                    patient,

                Name =
                    "Metformin",

                Dosage =
                    "500 mg",

                Form =
                    "Tablet",

                Instructions =
                    "Take as prescribed.",

                UnitsPerDose =
                    unitsPerDose,

                StartDate =
                    DateTime.UtcNow
                        .AddDays(
                            -30
                        ),

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow
                        .AddDays(
                            -30
                        )
            };
        }

        private static MedicationCollection
            Collection(
                Patient patient,
                Clinic clinic,
                DateTime collectedAt
            )
        {
            return new MedicationCollection
            {
                Id =
                    Guid.NewGuid(),

                PatientId =
                    patient.Id,

                Patient =
                    patient,

                ClinicId =
                    clinic.Id,

                Clinic =
                    clinic,

                ScheduledCollectionDate =
                    collectedAt,

                CollectedAt =
                    collectedAt,

                Status =
                    MedicationCollectionStatuses
                        .Collected,

                CreatedAt =
                    collectedAt
            };
        }

        private static MedicationCollectionItem
            CollectionItem(
                MedicationCollection collection,
                Medication medication,
                ClinicStock stock,
                int quantity
            )
        {
            return new MedicationCollectionItem
            {
                Id =
                    Guid.NewGuid(),

                MedicationCollectionId =
                    collection.Id,

                MedicationCollection =
                    collection,

                MedicationId =
                    medication.Id,

                Medication =
                    medication,

                ClinicStockId =
                    stock.Id,

                ClinicStock =
                    stock,

                Quantity =
                    quantity,

                CreatedAt =
                    collection
                        .CollectedAt ??
                    DateTime.UtcNow
            };
        }

        private static MedicationLog
            Log(
                Medication medication,
                DateTime takenAt,
                bool taken
            )
        {
            return new MedicationLog
            {
                Id =
                    Guid.NewGuid(),

                MedicationId =
                    medication.Id,

                Medication =
                    medication,

                TakenAt =
                    takenAt,

                Taken =
                    taken
            };
        }
    }
}
