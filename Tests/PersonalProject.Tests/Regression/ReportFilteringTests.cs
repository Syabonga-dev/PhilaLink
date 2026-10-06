using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Controllers;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Tests.Support;
using System.Security.Claims;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class ReportFilteringTests
    {
        [Fact]
        public async Task
            PatientReportAppliesClinicDateStatusAndSearchFilters()
        {
            await using var db =
                TestDb.Create();

            var today =
                DateTime.UtcNow
                    .Date;

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
                    "Clinic Admin A",
                    "1401"
                );

            var admin =
                TestDataFactory.ClinicAdmin(
                    adminUser,
                    clinicA
                );

            /*
             * This is the only row that should survive every
             * filter.
             */
            var targetUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Target Patient",
                    "1402"
                );

            targetUser.Email =
                "target.patient@philalink.test";

            var targetPatient =
                TestDataFactory.Patient(
                    targetUser,
                    clinicA,
                    "PHL-TARGET-001"
                );

            targetPatient.CreatedAt =
                today
                    .AddDays(
                        -5
                    )
                    .AddHours(
                        12
                    );

            /*
             * Same clinic and date range, but inactive.
             */
            var inactiveUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Target Inactive",
                    "1403"
                );

            inactiveUser.IsActive =
                false;

            var inactivePatient =
                TestDataFactory.Patient(
                    inactiveUser,
                    clinicA,
                    "PHL-TARGET-002"
                );

            inactivePatient.CreatedAt =
                today
                    .AddDays(
                        -4
                    )
                    .AddHours(
                        12
                    );

            /*
             * Active and matches the search text, but outside
             * the requested date range.
             */
            var oldUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Target Historical",
                    "1404"
                );

            var oldPatient =
                TestDataFactory.Patient(
                    oldUser,
                    clinicA,
                    "PHL-TARGET-003"
                );

            oldPatient.CreatedAt =
                today
                    .AddDays(
                        -40
                    );

            /*
             * Active, in range, and matches search, but belongs
             * to another clinic.
             */
            var otherClinicUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Target Other Clinic",
                    "1405"
                );

            var otherClinicPatient =
                TestDataFactory.Patient(
                    otherClinicUser,
                    clinicB,
                    "PHL-TARGET-004"
                );

            otherClinicPatient.CreatedAt =
                today
                    .AddDays(
                        -3
                    )
                    .AddHours(
                        12
                    );

            /*
             * Same clinic and date range, active, but does not
             * match the search filter.
             */
            var unrelatedUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Different Person",
                    "1406"
                );

            var unrelatedPatient =
                TestDataFactory.Patient(
                    unrelatedUser,
                    clinicA,
                    "PHL-DIFFERENT-001"
                );

            unrelatedPatient.CreatedAt =
                today
                    .AddDays(
                        -2
                    )
                    .AddHours(
                        12
                    );

            db.AddRange(
                clinicA,
                clinicB,
                adminUser,
                admin,
                targetUser,
                targetPatient,
                inactiveUser,
                inactivePatient,
                oldUser,
                oldPatient,
                otherClinicUser,
                otherClinicPatient,
                unrelatedUser,
                unrelatedPatient
            );

            await db.SaveChangesAsync();

            var controller =
                CreateController(
                    db,
                    adminUser.Id
                );

            var query =
                new ClinicAdminDynamicReportQueryDto
                {
                    ReportType =
                        "Patients",

                    DateFrom =
                        today.AddDays(
                            -10
                        ),

                    DateTo =
                        today,

                    Status =
                        "Active",

                    Search =
                        "Target"
                };

            var actionResult =
                await controller
                    .Preview(
                        query
                    );

            var ok =
                Assert.IsType<
                    OkObjectResult
                >(
                    actionResult
                );

            var report =
                Assert.IsType<
                    ClinicAdminDynamicReportPreviewDto
                >(
                    ok.Value
                );

            Assert.Equal(
                "Patients",
                report.ReportType
            );

            Assert.Equal(
                clinicA.Name,
                report.ClinicName
            );

            Assert.Equal(
                adminUser.FullName,
                report.RequestedBy
            );

            var row =
                Assert.Single(
                    report.Rows
                );

            Assert.Equal(
                targetPatient.PatientNumber,
                row[
                    "patientNumber"
                ]
            );

            Assert.Equal(
                targetUser.FullName,
                row[
                    "fullName"
                ]
            );

            Assert.Equal(
                "Active",
                row[
                    "status"
                ]
            );

            Assert.Equal(
                targetUser.Email,
                row[
                    "email"
                ]
            );

            var shownSummary =
                report.Summary
                    .Single(
                        item =>
                            item.Key ==
                            "shown"
                    );

            Assert.Equal(
                "1",
                shownSummary.Value
            );

            var activeSummary =
                report.Summary
                    .Single(
                        item =>
                            item.Key ==
                            "active"
                    );

            Assert.Equal(
                "1",
                activeSummary.Value
            );

            var inactiveSummary =
                report.Summary
                    .Single(
                        item =>
                            item.Key ==
                            "inactive"
                    );

            Assert.Equal(
                "0",
                inactiveSummary.Value
            );
        }

        [Fact]
        public async Task
            ClinicAdminCannotOverrideAuthenticatedClinicScope()
        {
            await using var db =
                TestDb.Create();

            var today =
                DateTime.UtcNow
                    .Date;

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
                    "Clinic Admin A",
                    "1410"
                );

            var admin =
                TestDataFactory.ClinicAdmin(
                    adminUser,
                    clinicA
                );

            var clinicAUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Clinic A Patient",
                    "1411"
                );

            var clinicAPatient =
                TestDataFactory.Patient(
                    clinicAUser,
                    clinicA,
                    "PHL-CLINIC-A"
                );

            clinicAPatient.CreatedAt =
                today
                    .AddDays(
                        -1
                    );

            var clinicBUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Clinic B Patient",
                    "1412"
                );

            var clinicBPatient =
                TestDataFactory.Patient(
                    clinicBUser,
                    clinicB,
                    "PHL-CLINIC-B"
                );

            clinicBPatient.CreatedAt =
                today
                    .AddDays(
                        -1
                    );

            db.AddRange(
                clinicA,
                clinicB,
                adminUser,
                admin,
                clinicAUser,
                clinicAPatient,
                clinicBUser,
                clinicBPatient
            );

            await db.SaveChangesAsync();

            var controller =
                CreateController(
                    db,
                    adminUser.Id
                );

            /*
             * A malicious or accidental ClinicId query value
             * points at Clinic B.
             *
             * ClinicAdminReportsController must ignore it and
             * resolve clinic scope from the authenticated admin.
             */
            var query =
                new ClinicAdminDynamicReportQueryDto
                {
                    ReportType =
                        "Patients",

                    ClinicId =
                        clinicB.Id,

                    DateFrom =
                        today.AddDays(
                            -10
                        ),

                    DateTo =
                        today
                };

            var actionResult =
                await controller
                    .Preview(
                        query
                    );

            var ok =
                Assert.IsType<
                    OkObjectResult
                >(
                    actionResult
                );

            var report =
                Assert.IsType<
                    ClinicAdminDynamicReportPreviewDto
                >(
                    ok.Value
                );

            Assert.Equal(
                clinicA.Name,
                report.ClinicName
            );

            var row =
                Assert.Single(
                    report.Rows
                );

            Assert.Equal(
                clinicAPatient.PatientNumber,
                row[
                    "patientNumber"
                ]
            );

            Assert.DoesNotContain(
                report.Rows,
                item =>
                    Equals(
                        item[
                            "patientNumber"
                        ],
                        clinicBPatient
                            .PatientNumber
                    )
            );
        }

        [Fact]
        public async Task
            InvalidReportDateRangeReturnsBadRequest()
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
                    "Clinic Admin A",
                    "1420"
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

            var controller =
                CreateController(
                    db,
                    adminUser.Id
                );

            var today =
                DateTime.UtcNow
                    .Date;

            var query =
                new ClinicAdminDynamicReportQueryDto
                {
                    ReportType =
                        "Patients",

                    DateFrom =
                        today,

                    DateTo =
                        today.AddDays(
                            -1
                        )
                };

            var result =
                await controller
                    .Preview(
                        query
                    );

            Assert.IsType<
                BadRequestObjectResult
            >(
                result
            );
        }

        private static ClinicAdminReportsController
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
                            RoleNames.ClinicAdmin
                        )
                    },
                    "Test"
                );

            var controller =
                new ClinicAdminReportsController(
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
    }
}
