using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalProject.Controllers;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using PersonalProject.Tests.Support;
using System.Security.Claims;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class ClinicAdminAssignmentWorkflowTests
    {
        [Fact]
        public async Task
            ClinicAdminCanBeAssignedReassignedDeassignedAndAssignedAgain()
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

            var superAdminUser =
                TestDataFactory.User(
                    RoleNames.SuperAdmin,
                    "Super Administrator",
                    "1601"
                );

            var superAdmin =
                TestDataFactory.SuperAdmin(
                    superAdminUser
                );

            var clinicAdminUser =
                TestDataFactory.User(
                    RoleNames.ClinicAdmin,
                    "Clinic Administrator",
                    "1602"
                );

            /*
             * Start deliberately unassigned so the complete
             * assignment lifecycle can be exercised.
             */
            var clinicAdmin =
                TestDataFactory.ClinicAdmin(
                    clinicAdminUser,
                    null
                );

            db.AddRange(
                clinicA,
                clinicB,
                superAdminUser,
                superAdmin,
                clinicAdminUser,
                clinicAdmin
            );

            await db.SaveChangesAsync();

            var audit =
                new RecordingAuditLogService();

            var controller =
                CreateController(
                    db,
                    audit,
                    superAdminUser.Id
                );

            // =================================================
            // 1. INITIAL ASSIGNMENT
            // =================================================

            var assignedResult =
                await controller
                    .AssignClinicAdmin(
                        clinicAdminUser.Id,
                        new AssignClinicAdminDto
                        {
                            ClinicId =
                                clinicA.Id
                        }
                    );

            Assert.IsType<
                OkObjectResult
            >(
                assignedResult
            );

            db.ChangeTracker.Clear();

            var afterAssignment =
                await db.Admins
                    .AsNoTracking()
                    .SingleAsync(
                        item =>
                            item.UserId ==
                            clinicAdminUser.Id
                    );

            Assert.Equal(
                clinicA.Id,
                afterAssignment.ClinicId
            );

            Assert.NotNull(
                afterAssignment.UpdatedAt
            );

            Assert.Contains(
                audit.Entries,
                entry =>
                    entry.Action ==
                        "ClinicAdminAssigned" &&
                    entry.ClinicId ==
                        clinicA.Id
            );

            AssertAssignmentEmailOutcomeLogged(
                audit,
                clinicA.Id
            );

            // =================================================
            // 2. REASSIGNMENT TO ANOTHER CLINIC
            // =================================================

            var reassignedResult =
                await controller
                    .AssignClinicAdmin(
                        clinicAdminUser.Id,
                        new AssignClinicAdminDto
                        {
                            ClinicId =
                                clinicB.Id
                        }
                    );

            Assert.IsType<
                OkObjectResult
            >(
                reassignedResult
            );

            db.ChangeTracker.Clear();

            var afterReassignment =
                await db.Admins
                    .AsNoTracking()
                    .SingleAsync(
                        item =>
                            item.UserId ==
                            clinicAdminUser.Id
                    );

            Assert.Equal(
                clinicB.Id,
                afterReassignment.ClinicId
            );

            Assert.Contains(
                audit.Entries,
                entry =>
                    entry.Action ==
                        "ClinicAdminReassigned" &&
                    entry.ClinicId ==
                        clinicB.Id
            );

            Assert.Contains(
                audit.Entries,
                entry =>
                    entry.Action ==
                        "ClinicAdminReassigned" &&
                    entry.Details.Contains(
                        clinicA.Name,
                        StringComparison
                            .OrdinalIgnoreCase
                    ) &&
                    entry.Details.Contains(
                        clinicB.Name,
                        StringComparison
                            .OrdinalIgnoreCase
                    )
            );

            // =================================================
            // 3. DEASSIGNMENT
            // =================================================

            var deassignedResult =
                await controller
                    .DeassignClinicAdmin(
                        clinicAdminUser.Id
                    );

            Assert.IsType<
                OkObjectResult
            >(
                deassignedResult
            );

            db.ChangeTracker.Clear();

            var afterDeassignment =
                await db.Admins
                    .AsNoTracking()
                    .SingleAsync(
                        item =>
                            item.UserId ==
                            clinicAdminUser.Id
                    );

            Assert.Null(
                afterDeassignment.ClinicId
            );

            Assert.Contains(
                audit.Entries,
                entry =>
                    entry.Action ==
                        "ClinicAdminDeassigned" &&
                    entry.ClinicId ==
                        clinicB.Id
            );

            Assert.Contains(
                audit.Entries,
                entry =>
                    entry.Action ==
                        "ClinicAdminDeassigned" &&
                    entry.Details.Contains(
                        clinicB.Name,
                        StringComparison
                            .OrdinalIgnoreCase
                    )
            );

            AssertDeassignmentEmailOutcomeLogged(
                audit,
                clinicB.Id
            );

            // =================================================
            // 4. ASSIGN AGAIN AFTER DEASSIGNMENT
            // =================================================

            var assignedAgainResult =
                await controller
                    .AssignClinicAdmin(
                        clinicAdminUser.Id,
                        new AssignClinicAdminDto
                        {
                            ClinicId =
                                clinicA.Id
                        }
                    );

            Assert.IsType<
                OkObjectResult
            >(
                assignedAgainResult
            );

            db.ChangeTracker.Clear();

            var finalAdmin =
                await db.Admins
                    .AsNoTracking()
                    .SingleAsync(
                        item =>
                            item.UserId ==
                            clinicAdminUser.Id
                    );

            Assert.Equal(
                clinicA.Id,
                finalAdmin.ClinicId
            );

            /*
             * Because the account was unassigned immediately
             * before this operation, the final operation must
             * be treated as a fresh assignment rather than a
             * reassignment.
             */
            Assert.Equal(
                2,
                audit.Entries.Count(
                    entry =>
                        entry.Action ==
                        "ClinicAdminAssigned"
                )
            );

            Assert.Equal(
                1,
                audit.Entries.Count(
                    entry =>
                        entry.Action ==
                        "ClinicAdminReassigned"
                )
            );

            Assert.Equal(
                1,
                audit.Entries.Count(
                    entry =>
                        entry.Action ==
                        "ClinicAdminDeassigned"
                )
            );
        }

        [Fact]
        public async Task
            ClinicAdminCannotBeAssignedToInactiveClinic()
        {
            await using var db =
                TestDb.Create();

            var activeClinic =
                TestDataFactory.Clinic(
                    "Active Clinic"
                );

            var inactiveClinic =
                TestDataFactory.Clinic(
                    "Inactive Clinic"
                );

            inactiveClinic.IsActive =
                false;

            var superAdminUser =
                TestDataFactory.User(
                    RoleNames.SuperAdmin,
                    "Super Administrator",
                    "1610"
                );

            var superAdmin =
                TestDataFactory.SuperAdmin(
                    superAdminUser
                );

            var clinicAdminUser =
                TestDataFactory.User(
                    RoleNames.ClinicAdmin,
                    "Clinic Administrator",
                    "1611"
                );

            var clinicAdmin =
                TestDataFactory.ClinicAdmin(
                    clinicAdminUser,
                    activeClinic
                );

            db.AddRange(
                activeClinic,
                inactiveClinic,
                superAdminUser,
                superAdmin,
                clinicAdminUser,
                clinicAdmin
            );

            await db.SaveChangesAsync();

            var audit =
                new RecordingAuditLogService();

            var controller =
                CreateController(
                    db,
                    audit,
                    superAdminUser.Id
                );

            var result =
                await controller
                    .AssignClinicAdmin(
                        clinicAdminUser.Id,
                        new AssignClinicAdminDto
                        {
                            ClinicId =
                                inactiveClinic.Id
                        }
                    );

            Assert.IsType<
                BadRequestObjectResult
            >(
                result
            );

            db.ChangeTracker.Clear();

            var persisted =
                await db.Admins
                    .AsNoTracking()
                    .SingleAsync(
                        item =>
                            item.UserId ==
                            clinicAdminUser.Id
                    );

            /*
             * A rejected assignment must not remove or change
             * the existing valid assignment.
             */
            Assert.Equal(
                activeClinic.Id,
                persisted.ClinicId
            );

            Assert.DoesNotContain(
                audit.Entries,
                entry =>
                    entry.Action ==
                        "ClinicAdminAssigned" ||
                    entry.Action ==
                        "ClinicAdminReassigned"
            );
        }

        [Fact]
        public async Task
            AssigningSameClinicRecordsConfirmationInsteadOfReassignment()
        {
            await using var db =
                TestDb.Create();

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var superAdminUser =
                TestDataFactory.User(
                    RoleNames.SuperAdmin,
                    "Super Administrator",
                    "1620"
                );

            var superAdmin =
                TestDataFactory.SuperAdmin(
                    superAdminUser
                );

            var clinicAdminUser =
                TestDataFactory.User(
                    RoleNames.ClinicAdmin,
                    "Clinic Administrator",
                    "1621"
                );

            var clinicAdmin =
                TestDataFactory.ClinicAdmin(
                    clinicAdminUser,
                    clinic
                );

            db.AddRange(
                clinic,
                superAdminUser,
                superAdmin,
                clinicAdminUser,
                clinicAdmin
            );

            await db.SaveChangesAsync();

            var audit =
                new RecordingAuditLogService();

            var controller =
                CreateController(
                    db,
                    audit,
                    superAdminUser.Id
                );

            var result =
                await controller
                    .AssignClinicAdmin(
                        clinicAdminUser.Id,
                        new AssignClinicAdminDto
                        {
                            ClinicId =
                                clinic.Id
                        }
                    );

            Assert.IsType<
                OkObjectResult
            >(
                result
            );

            db.ChangeTracker.Clear();

            var persisted =
                await db.Admins
                    .AsNoTracking()
                    .SingleAsync(
                        item =>
                            item.UserId ==
                            clinicAdminUser.Id
                    );

            Assert.Equal(
                clinic.Id,
                persisted.ClinicId
            );

            Assert.Contains(
                audit.Entries,
                entry =>
                    entry.Action ==
                    "ClinicAdminAssignmentConfirmed"
            );

            Assert.DoesNotContain(
                audit.Entries,
                entry =>
                    entry.Action ==
                    "ClinicAdminReassigned"
            );
        }

        [Fact]
        public async Task
            DeassigningAlreadyUnassignedClinicAdminIsIdempotent()
        {
            await using var db =
                TestDb.Create();

            var superAdminUser =
                TestDataFactory.User(
                    RoleNames.SuperAdmin,
                    "Super Administrator",
                    "1630"
                );

            var superAdmin =
                TestDataFactory.SuperAdmin(
                    superAdminUser
                );

            var clinicAdminUser =
                TestDataFactory.User(
                    RoleNames.ClinicAdmin,
                    "Unassigned Administrator",
                    "1631"
                );

            var clinicAdmin =
                TestDataFactory.ClinicAdmin(
                    clinicAdminUser,
                    null
                );

            db.AddRange(
                superAdminUser,
                superAdmin,
                clinicAdminUser,
                clinicAdmin
            );

            await db.SaveChangesAsync();

            var audit =
                new RecordingAuditLogService();

            var controller =
                CreateController(
                    db,
                    audit,
                    superAdminUser.Id
                );

            var result =
                await controller
                    .DeassignClinicAdmin(
                        clinicAdminUser.Id
                    );

            Assert.IsType<
                OkObjectResult
            >(
                result
            );

            db.ChangeTracker.Clear();

            var persisted =
                await db.Admins
                    .AsNoTracking()
                    .SingleAsync(
                        item =>
                            item.UserId ==
                            clinicAdminUser.Id
                    );

            Assert.Null(
                persisted.ClinicId
            );

            /*
             * No assignment mutation occurred, therefore no
             * deassignment audit event should be produced.
             */
            Assert.DoesNotContain(
                audit.Entries,
                entry =>
                    entry.Action ==
                    "ClinicAdminDeassigned"
            );
        }

        private static SuperAdminController
            CreateController(
                PersonalProject.Data
                    .PhilaLinkDbContext db,
                IAuditLogService audit,
                Guid superAdminUserId
            )
        {
            /*
             * Email configuration is intentionally empty.
             *
             * The production controller catches assignment
             * email delivery failures, allowing the assignment
             * itself to remain successful while recording the
             * delivery outcome in the audit trail.
             */
            var configuration =
                new ConfigurationBuilder()
                    .Build();

            var controller =
                new SuperAdminController(
                    db,
                    audit,
                    configuration,
                    NullLogger<
                        SuperAdminController
                    >.Instance
                );

            var identity =
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(
                            ClaimTypes
                                .NameIdentifier,
                            superAdminUserId
                                .ToString()
                        ),

                        new Claim(
                            ClaimTypes.Role,
                            RoleNames.SuperAdmin
                        )
                    },
                    "Test"
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

        private static void
            AssertAssignmentEmailOutcomeLogged(
                RecordingAuditLogService audit,
                Guid clinicId
            )
        {
            Assert.Contains(
                audit.Entries,
                entry =>
                    (
                        entry.Action ==
                            "ClinicAdminAssignmentEmailSent" ||
                        entry.Action ==
                            "ClinicAdminAssignmentEmailDeliveryFailed"
                    ) &&
                    entry.ClinicId ==
                        clinicId
            );
        }

        private static void
            AssertDeassignmentEmailOutcomeLogged(
                RecordingAuditLogService audit,
                Guid clinicId
            )
        {
            Assert.Contains(
                audit.Entries,
                entry =>
                    (
                        entry.Action ==
                            "ClinicAdminDeassignmentEmailSent" ||
                        entry.Action ==
                            "ClinicAdminDeassignmentEmailDeliveryFailed"
                    ) &&
                    entry.ClinicId ==
                        clinicId
            );
        }

        private sealed class
            RecordingAuditLogService :
            IAuditLogService
        {
            public List<RecordedAuditEntry>
                Entries
            {
                get;
            } = new();

            public Task LogAsync(
                string action,
                Guid userId,
                string? details = null,
                Guid? clinicId = null
            )
            {
                Entries.Add(
                    new RecordedAuditEntry
                    {
                        Action =
                            action,

                        UserId =
                            userId,

                        Details =
                            details ??
                            string.Empty,

                        ClinicId =
                            clinicId
                    }
                );

                return Task.CompletedTask;
            }

            public Task<
                List<
                    AuditLogResponseDto
                >
            >
                GetVisibleLogsAsync(
                    Guid requestingUserId
                )
            {
                return Task.FromResult(
                    new List<
                        AuditLogResponseDto
                    >()
                );
            }
        }

        private sealed class
            RecordedAuditEntry
        {
            public string Action
            {
                get;
                init;
            } =
                string.Empty;

            public Guid UserId
            {
                get;
                init;
            }

            public string Details
            {
                get;
                init;
            } =
                string.Empty;

            public Guid? ClinicId
            {
                get;
                init;
            }
        }
    }
}
