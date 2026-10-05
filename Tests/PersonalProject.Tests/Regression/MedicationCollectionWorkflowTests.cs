using Microsoft.EntityFrameworkCore;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Implementations;
using PersonalProject.Tests.Support;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class MedicationCollectionWorkflowTests
    {
        [Fact]
        public async Task
            NurseCanScheduleCollectionForOwnClinicPatient()
        {
            await using var db =
                TestDb.Create();

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var nurseUser =
                TestDataFactory.User(
                    RoleNames.Nurse,
                    "Nurse One",
                    "1101"
                );

            var nurse =
                TestDataFactory.Nurse(
                    nurseUser,
                    clinic,
                    "1101"
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "1102"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinic,
                    "PHL-TEST-1102"
                );

            var stock =
                Stock(
                    clinic
                );

            var medication =
                Medication(
                    patient
                );

            db.AddRange(
                clinic,
                nurseUser,
                nurse,
                patientUser,
                patient,
                stock,
                medication
            );

            await db.SaveChangesAsync();

            var service =
                new MedicationCollectionService(
                    db,
                    NoOpAuditLogService
                        .Instance
                );

            var result =
                await service.CreateAsync(
                    new CreateMedicationCollectionDto
                    {
                        PatientId =
                            patient.Id,

                        ClinicId =
                            clinic.Id,

                        ScheduledCollectionDate =
                            DateTime.UtcNow
                                .AddDays(
                                    3
                                ),

                        Items =
                        {
                            new CreateMedicationCollectionItemDto
                            {
                                MedicationId =
                                    medication.Id,

                                ClinicStockId =
                                    stock.Id,

                                Quantity =
                                    30
                            }
                        }
                    },
                    nurseUser.Id
                );

            Assert.Equal(
                patient.Id,
                result.PatientId
            );

            Assert.Equal(
                clinic.Id,
                result.ClinicId
            );

            Assert.Single(
                result.Items
            );

            Assert.Equal(
                30,
                result.Items[0]
                    .Quantity
            );

            Assert.Equal(
                1,
                await db
                    .MedicationCollections
                    .CountAsync()
            );
        }

        [Fact]
        public async Task
            NurseCannotScheduleCollectionForOtherClinicPatient()
        {
            await using var db =
                TestDb.Create();

            var clinicA =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var clinicB =
                TestDataFactory.Clinic(
                    "Clinic B"
                );

            var nurseUser =
                TestDataFactory.User(
                    RoleNames.Nurse,
                    "Nurse One",
                    "1103"
                );

            var nurse =
                TestDataFactory.Nurse(
                    nurseUser,
                    clinicA,
                    "1103"
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient Two",
                    "1104"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinicB,
                    "PHL-TEST-1104"
                );

            db.AddRange(
                clinicA,
                clinicB,
                nurseUser,
                nurse,
                patientUser,
                patient
            );

            await db.SaveChangesAsync();

            var service =
                new MedicationCollectionService(
                    db,
                    NoOpAuditLogService
                        .Instance
                );

            await Assert.ThrowsAsync<
                UnauthorizedAccessException
            >(
                () =>
                    service.CreateAsync(
                        new CreateMedicationCollectionDto
                        {
                            PatientId =
                                patient.Id,

                            ClinicId =
                                clinicA.Id,

                            ScheduledCollectionDate =
                                DateTime.UtcNow
                                    .AddDays(
                                        3
                                    ),

                            Items =
                            {
                                new CreateMedicationCollectionItemDto
                                {
                                    MedicationId =
                                        Guid.NewGuid(),

                                    ClinicStockId =
                                        Guid.NewGuid(),

                                    Quantity =
                                        30
                                }
                            }
                        },
                        nurseUser.Id
                    )
            );
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
                    100,

                ReorderLevel =
                    20,

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow
            };
        }

        private static Medication
            Medication(
                Patient patient
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
                    "Take daily.",

                UnitsPerDose =
                    1,

                StartDate =
                    DateTime.UtcNow
                        .AddDays(
                            -10
                        ),

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow
            };
        }
    }
}
