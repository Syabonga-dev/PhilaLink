using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Implementations;
using PersonalProject.Tests.Support;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class ClinicStockWorkflowTests
    {
        [Fact]
        public async Task
            ClinicAdminCanCreateStockForOwnClinic()
        {
            await using var db =
                TestDb.Create();

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var adminUser =
                TestDataFactory.User(
                    RoleNames.ClinicAdmin,
                    "Clinic Admin",
                    "901"
                );

            var admin =
                TestDataFactory.ClinicAdmin(
                    adminUser,
                    clinic
                );

            db.AddRange(
                clinic,
                adminUser,
                admin
            );

            await db.SaveChangesAsync();

            var service =
                new ClinicStockService(
                    db,
                    NoOpAuditLogService
                        .Instance
                );

            var result =
                await service.CreateAsync(
                    new CreateClinicStockDto
                    {
                        ClinicId =
                            clinic.Id,

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
                            20
                    },
                    adminUser.Id
                );

            Assert.Equal(
                clinic.Id,
                result.ClinicId
            );

            Assert.Equal(
                100,
                result.QuantityOnHand
            );

            Assert.False(
                result.IsLowStock
            );
        }

        [Fact]
        public async Task
            ClinicAdminCannotCreateStockForAnotherClinic()
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

            var adminUser =
                TestDataFactory.User(
                    RoleNames.ClinicAdmin,
                    "Clinic Admin",
                    "902"
                );

            var admin =
                TestDataFactory.ClinicAdmin(
                    adminUser,
                    clinicA
                );

            db.AddRange(
                clinicA,
                clinicB,
                adminUser,
                admin
            );

            await db.SaveChangesAsync();

            var service =
                new ClinicStockService(
                    db,
                    NoOpAuditLogService
                        .Instance
                );

            await Assert.ThrowsAsync<
                UnauthorizedAccessException
            >(
                () =>
                    service.CreateAsync(
                        new CreateClinicStockDto
                        {
                            ClinicId =
                                clinicB.Id,

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
                                20
                        },
                        adminUser.Id
                    )
            );
        }

        [Fact]
        public async Task
            StockAdjustmentCannotBecomeNegative()
        {
            await using var db =
                TestDb.Create();

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var adminUser =
                TestDataFactory.User(
                    RoleNames.ClinicAdmin,
                    "Clinic Admin",
                    "903"
                );

            var admin =
                TestDataFactory.ClinicAdmin(
                    adminUser,
                    clinic
                );

            db.AddRange(
                clinic,
                adminUser,
                admin
            );

            await db.SaveChangesAsync();

            var service =
                new ClinicStockService(
                    db,
                    NoOpAuditLogService
                        .Instance
                );

            var stock =
                await service.CreateAsync(
                    new CreateClinicStockDto
                    {
                        ClinicId =
                            clinic.Id,

                        MedicationName =
                            "Metformin",

                        Strength =
                            "500 mg",

                        Form =
                            "Tablet",

                        Unit =
                            "tablets",

                        QuantityOnHand =
                            10,

                        ReorderLevel =
                            5
                    },
                    adminUser.Id
                );

            await Assert.ThrowsAsync<
                InvalidOperationException
            >(
                () =>
                    service.AdjustAsync(
                        stock.Id,
                        new AdjustClinicStockDto
                        {
                            QuantityChange =
                                -11,

                            Reason =
                                "Test"
                        },
                        adminUser.Id
                    )
            );
        }
    }
}
