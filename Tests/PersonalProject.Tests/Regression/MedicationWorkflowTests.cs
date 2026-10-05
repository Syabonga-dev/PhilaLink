using Microsoft.EntityFrameworkCore;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Implementations;
using PersonalProject.Tests.Support;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class MedicationWorkflowTests
    {
        [Fact]
        public async Task
            MedicationIdentityComesFromClinicInventory()
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
                    "1001"
                );

            var nurse =
                TestDataFactory.Nurse(
                    nurseUser,
                    clinic,
                    "1001"
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "1002"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinic,
                    "PHL-TEST-1002"
                );

            var stock =
                Stock(
                    clinic
                );

            db.AddRange(
                clinic,
                nurseUser,
                nurse,
                patientUser,
                patient,
                stock
            );

            await db.SaveChangesAsync();

            var service =
                new MedicationService(
                    db,
                    NoOpAuditLogService
                        .Instance
                );

            var medication =
                await service
                    .CreateMedicationAsync(
                        new MedicationCreateDto
                        {
                            PatientId =
                                patient.Id,

                            ClinicStockId =
                                stock.Id,

                            Name =
                                "Untrusted name",

                            Dosage =
                                "Untrusted dosage",

                            Form =
                                "Untrusted form",

                            Instructions =
                                "Take with food.",

                            UnitsPerDose =
                                1,

                            StartDate =
                                DateTime.UtcNow
                                    .AddDays(
                                        -1
                                    )
                        },
                        nurseUser.Id
                    );

            Assert.Equal(
                stock.MedicationName,
                medication.Name
            );

            Assert.Equal(
                stock.Strength,
                medication.Dosage
            );

            Assert.Equal(
                stock.Form,
                medication.Form
            );
        }

        [Fact]
        public async Task
            DuplicateMedicationScheduleIsRejected()
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
                    "1003"
                );

            var nurse =
                TestDataFactory.Nurse(
                    nurseUser,
                    clinic,
                    "1003"
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "1004"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinic,
                    "PHL-TEST-1004"
                );

            var stock =
                Stock(
                    clinic
                );

            db.AddRange(
                clinic,
                nurseUser,
                nurse,
                patientUser,
                patient,
                stock
            );

            await db.SaveChangesAsync();

            var service =
                new MedicationService(
                    db,
                    NoOpAuditLogService
                        .Instance
                );

            var medication =
                await service
                    .CreateMedicationAsync(
                        new MedicationCreateDto
                        {
                            PatientId =
                                patient.Id,

                            ClinicStockId =
                                stock.Id,

                            Instructions =
                                "Take daily.",

                            UnitsPerDose =
                                1,

                            StartDate =
                                DateTime.UtcNow
                                    .AddDays(
                                        -1
                                    )
                        },
                        nurseUser.Id
                    );

            await service.AddScheduleAsync(
                medication.Id,
                "08:00",
                nurseUser.Id
            );

            Assert.Equal(
                1,
                await db
                    .MedicationSchedules
                    .CountAsync()
            );

            await Assert.ThrowsAsync<
                InvalidOperationException
            >(
                () =>
                    service.AddScheduleAsync(
                        medication.Id,
                        "08:00",
                        nurseUser.Id
                    )
            );
        }

        [Fact]
        public async Task
            NurseCannotCreateMedicationForOtherClinicPatient()
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
                    "1005"
                );

            var nurse =
                TestDataFactory.Nurse(
                    nurseUser,
                    clinicA,
                    "1005"
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient Two",
                    "1006"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinicB,
                    "PHL-TEST-1006"
                );

            var stock =
                Stock(
                    clinicA
                );

            db.AddRange(
                clinicA,
                clinicB,
                nurseUser,
                nurse,
                patientUser,
                patient,
                stock
            );

            await db.SaveChangesAsync();

            var service =
                new MedicationService(
                    db,
                    NoOpAuditLogService
                        .Instance
                );

            await Assert.ThrowsAsync<
                UnauthorizedAccessException
            >(
                () =>
                    service
                        .CreateMedicationAsync(
                            new MedicationCreateDto
                            {
                                PatientId =
                                    patient.Id,

                                ClinicStockId =
                                    stock.Id,

                                Instructions =
                                    "Take daily.",

                                UnitsPerDose =
                                    1
                            },
                            nurseUser.Id
                        )
            );
        }

        [Fact]
        public async Task
            PatientCannotLogAnotherPatientsMedication()
        {
            await using var db =
                TestDb.Create();

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var patientAUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient A",
                    "1007"
                );

            var patientA =
                TestDataFactory.Patient(
                    patientAUser,
                    clinic,
                    "PHL-TEST-1007"
                );

            var patientBUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient B",
                    "1008"
                );

            var patientB =
                TestDataFactory.Patient(
                    patientBUser,
                    clinic,
                    "PHL-TEST-1008"
                );

            var medication =
                new Medication
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        patientB.Id,

                    Patient =
                        patientB,

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
                                -1
                            ),

                    IsActive =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };

            db.AddRange(
                clinic,
                patientAUser,
                patientA,
                patientBUser,
                patientB,
                medication
            );

            await db.SaveChangesAsync();

            var service =
                new MedicationService(
                    db,
                    NoOpAuditLogService
                        .Instance
                );

            await Assert.ThrowsAsync<
                KeyNotFoundException
            >(
                () =>
                    service
                        .LogPatientMedicationAsync(
                            patientAUser.Id,
                            medication.Id,
                            true,
                            null
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
    }
}
