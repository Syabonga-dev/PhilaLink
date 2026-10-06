using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Implementations;
using PersonalProject.Tests.Support;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class StaffOnboardingWorkflowTests
    {
        [Fact]
        public async Task
            ClinicAdminCreatesNurseWithSecureTemporaryOnboardingState()
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
                    "1501"
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
                CreateService(
                    db
                );

            var dto =
                NurseRegistration(
                    clinic.Id,
                    "Nurse One",
                    "9001015000001",
                    "0715000001",
                    "NUR-1501",
                    "SANC-1501"
                );

            var result =
                await service
                    .RegisterNurseAsync(
                        dto,
                        adminUser.Id
                    );

            Assert.Equal(
                RoleNames.Nurse,
                result.Role
            );

            Assert.Equal(
                clinic.Id,
                result.ClinicId
            );

            Assert.Equal(
                clinic.Name,
                result.ClinicName
            );

            Assert.Equal(
                dto.Email
                    .ToLowerInvariant(),
                result.Email
            );

            var user =
                await db.Users
                    .Include(
                        item =>
                            item.Nurse
                    )
                    .SingleAsync(
                        item =>
                            item.Id ==
                            result.UserId
                    );

            Assert.Equal(
                RoleNames.Nurse,
                user.Role
            );

            Assert.True(
                user.IsActive
            );

            Assert.True(
                user.IsVerified
            );

            Assert.NotNull(
                user.VerifiedAt
            );

            /*
             * Administrator-created workforce accounts must
             * always begin in restricted first-login mode.
             */
            Assert.True(
                user.MustChangePassword
            );

            /*
             * The stored credential must be a BCrypt hash,
             * never a plaintext temporary password.
             */
            Assert.StartsWith(
                "$2",
                user.PasswordHash
            );

            Assert.NotNull(
                user.Nurse
            );

            Assert.Equal(
                clinic.Id,
                user.Nurse!
                    .ClinicId
            );

            Assert.Equal(
                dto.EmployeeNumber,
                user.Nurse
                    .EmployeeNumber
            );

            Assert.Equal(
                dto.RegistrationNumber,
                user.Nurse
                    .RegistrationNumber
            );

            /*
             * Plaintext temporary credentials must never be
             * exposed through NewStaffAccountDto.
             */
            Assert.Null(
                typeof(
                    NewStaffAccountDto
                )
                .GetProperty(
                    "TemporaryPassword"
                )
            );

            Assert.Null(
                typeof(
                    NewStaffAccountDto
                )
                .GetProperty(
                    "Password"
                )
            );
        }

        [Fact]
        public async Task
            ClinicAdminCreatesProxyInOwnClinicWithRestrictedFirstLogin()
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
                    "1510"
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
                CreateService(
                    db
                );

            var dto =
                ProxyRegistration(
                    clinic.Id,
                    "Proxy One",
                    "9001015000011",
                    "0715000011"
                );

            var result =
                await service
                    .RegisterProxyAsync(
                        dto,
                        adminUser.Id
                    );

            Assert.Equal(
                RoleNames.Proxy,
                result.Role
            );

            Assert.Equal(
                clinic.Id,
                result.ClinicId
            );

            var user =
                await db.Users
                    .Include(
                        item =>
                            item.Proxy
                    )
                    .SingleAsync(
                        item =>
                            item.Id ==
                            result.UserId
                    );

            Assert.True(
                user.IsActive
            );

            Assert.True(
                user.IsVerified
            );

            Assert.True(
                user.MustChangePassword
            );

            Assert.StartsWith(
                "$2",
                user.PasswordHash
            );

            Assert.NotNull(
                user.Proxy
            );

            Assert.Equal(
                clinic.Id,
                user.Proxy!
                    .ClinicId
            );

            Assert.Equal(
                dto.Email
                    .ToLowerInvariant(),
                user.Email
            );

            Assert.Equal(
                dto.Email
                    .ToLowerInvariant(),
                user.Proxy
                    .Email
            );
        }

        [Fact]
        public async Task
            SuperAdminCreatesClinicAdminWithAssignedClinic()
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
                    "Super Admin",
                    "1520"
                );

            var superAdmin =
                TestDataFactory.SuperAdmin(
                    superAdminUser
                );

            db.AddRange(
                clinic,
                superAdminUser,
                superAdmin
            );

            await db.SaveChangesAsync();

            var service =
                CreateService(
                    db
                );

            var dto =
                new RegisterClinicAdminDto
                {
                    FullName =
                        "New Clinic Admin",

                    IdNumber =
                        "9001015000021",

                    PhoneNumber =
                        "0715000021",

                    Email =
                        "NEW.ADMIN@philalink.test",

                    ClinicId =
                        clinic.Id
                };

            var result =
                await service
                    .RegisterClinicAdminAsync(
                        dto,
                        superAdminUser.Id
                    );

            Assert.Equal(
                RoleNames.ClinicAdmin,
                result.Role
            );

            Assert.Equal(
                clinic.Id,
                result.ClinicId
            );

            Assert.Equal(
                clinic.Name,
                result.ClinicName
            );

            var user =
                await db.Users
                    .Include(
                        item =>
                            item.Admin
                    )
                    .SingleAsync(
                        item =>
                            item.Id ==
                            result.UserId
                    );

            Assert.True(
                user.IsActive
            );

            Assert.True(
                user.IsVerified
            );

            Assert.True(
                user.MustChangePassword
            );

            Assert.StartsWith(
                "$2",
                user.PasswordHash
            );

            Assert.Equal(
                "new.admin@philalink.test",
                user.Email
            );

            Assert.NotNull(
                user.Admin
            );

            Assert.Equal(
                clinic.Id,
                user.Admin!
                    .ClinicId
            );
        }

        [Fact]
        public async Task
            ClinicAdminCannotCreateNurseForAnotherClinic()
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
                    "Clinic Admin A",
                    "1530"
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
                CreateService(
                    db
                );

            var dto =
                NurseRegistration(
                    clinicB.Id,
                    "Cross Clinic Nurse",
                    "9001015000031",
                    "0715000031",
                    "NUR-1531",
                    "SANC-1531"
                );

            await Assert.ThrowsAsync<
                UnauthorizedAccessException
            >(
                () =>
                    service
                        .RegisterNurseAsync(
                            dto,
                            adminUser.Id
                        )
            );

            Assert.False(
                await db.Users
                    .AnyAsync(
                        item =>
                            item.Email ==
                            dto.Email
                                .ToLowerInvariant()
                    )
            );
        }

        [Fact]
        public async Task
            DuplicateIdentityEmailOrPhoneIsRejected()
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
                    "1540"
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
                CreateService(
                    db
                );

            var first =
                NurseRegistration(
                    clinic.Id,
                    "Nurse One",
                    "9001015000041",
                    "0715000041",
                    "NUR-1541",
                    "SANC-1541"
                );

            await service
                .RegisterNurseAsync(
                    first,
                    adminUser.Id
                );

            var duplicateEmail =
                ProxyRegistration(
                    clinic.Id,
                    "Proxy Duplicate",
                    "9001015000042",
                    "0715000042"
                );

            duplicateEmail.Email =
                first.Email
                    .ToUpperInvariant();

            await Assert.ThrowsAsync<
                InvalidOperationException
            >(
                () =>
                    service
                        .RegisterProxyAsync(
                            duplicateEmail,
                            adminUser.Id
                        )
            );

            Assert.Equal(
                2,
                await db.Users
                    .CountAsync()
            );
        }

        [Fact]
        public async Task
            ResendInvitationReplacesPreviousTemporaryCredential()
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
                    "1550"
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
                CreateService(
                    db
                );

            var registration =
                NurseRegistration(
                    clinic.Id,
                    "Nurse One",
                    "9001015000051",
                    "0715000051",
                    "NUR-1551",
                    "SANC-1551"
                );

            var created =
                await service
                    .RegisterNurseAsync(
                        registration,
                        adminUser.Id
                    );

            var user =
                await db.Users
                    .SingleAsync(
                        item =>
                            item.Id ==
                            created.UserId
                    );

            var originalHash =
                user.PasswordHash;

            Assert.True(
                user.MustChangePassword
            );

            var resent =
                await service
                    .ResendAccountInvitationAsync(
                        user.Id,
                        adminUser.Id
                    );

            await db.Entry(
                    user
                )
                .ReloadAsync();

            Assert.Equal(
                user.Id,
                resent.UserId
            );

            Assert.True(
                user.MustChangePassword
            );

            /*
             * Resending must invalidate the previous temporary
             * credential by replacing its stored hash.
             */
            Assert.NotEqual(
                originalHash,
                user.PasswordHash
            );

            Assert.StartsWith(
                "$2",
                user.PasswordHash
            );

            Assert.Null(
                typeof(
                    NewStaffAccountDto
                )
                .GetProperty(
                    "TemporaryPassword"
                )
            );
        }

        [Fact]
        public async Task
            InvitationCannotBeResentAfterFirstLoginSetupCompleted()
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
                    "1560"
                );

            var admin =
                TestDataFactory.ClinicAdmin(
                    adminUser,
                    clinic
                );

            var nurseUser =
                TestDataFactory.User(
                    RoleNames.Nurse,
                    "Completed Nurse",
                    "1561"
                );

            nurseUser.MustChangePassword =
                false;

            var nurse =
                TestDataFactory.Nurse(
                    nurseUser,
                    clinic,
                    "1561"
                );

            db.AddRange(
                clinic,
                adminUser,
                admin,
                nurseUser,
                nurse
            );

            await db.SaveChangesAsync();

            var service =
                CreateService(
                    db
                );

            await Assert.ThrowsAsync<
                InvalidOperationException
            >(
                () =>
                    service
                        .ResendAccountInvitationAsync(
                            nurseUser.Id,
                            adminUser.Id
                        )
            );
        }

        [Fact]
        public async Task
            InactiveAccountCannotReceiveResentInvitation()
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
                    "1570"
                );

            var admin =
                TestDataFactory.ClinicAdmin(
                    adminUser,
                    clinic
                );

            var nurseUser =
                TestDataFactory.User(
                    RoleNames.Nurse,
                    "Inactive Nurse",
                    "1571"
                );

            nurseUser.IsActive =
                false;

            nurseUser.MustChangePassword =
                true;

            var nurse =
                TestDataFactory.Nurse(
                    nurseUser,
                    clinic,
                    "1571"
                );

            db.AddRange(
                clinic,
                adminUser,
                admin,
                nurseUser,
                nurse
            );

            await db.SaveChangesAsync();

            var service =
                CreateService(
                    db
                );

            await Assert.ThrowsAsync<
                InvalidOperationException
            >(
                () =>
                    service
                        .ResendAccountInvitationAsync(
                            nurseUser.Id,
                            adminUser.Id
                        )
            );
        }

        private static AdminService
            CreateService(
                PersonalProject.Data
                    .PhilaLinkDbContext db
            )
        {
            /*
             * No SMTP configuration is deliberately provided.
             *
             * AdminService catches invitation-delivery failure,
             * allowing these tests to validate account creation
             * without sending real emails.
             */
            var configuration =
                new ConfigurationBuilder()
                    .Build();

            return new AdminService(
                db,
                configuration,
                NullLogger<
                    AdminService
                >.Instance
            );
        }

        private static RegisterNurseDto
            NurseRegistration(
                Guid clinicId,
                string fullName,
                string idNumber,
                string phone,
                string employeeNumber,
                string registrationNumber
            )
        {
            return new RegisterNurseDto
            {
                FullName =
                    fullName,

                IdNumber =
                    idNumber,

                PhoneNumber =
                    phone,

                Email =
                    $"{employeeNumber.ToLowerInvariant()}@philalink.test",

                EmployeeNumber =
                    employeeNumber,

                RegistrationNumber =
                    registrationNumber,

                Qualification =
                    "Registered Nurse",

                ClinicId =
                    clinicId,

                AddressLine1 =
                    "10 Test Street",

                AddressLine2 =
                    null,

                Suburb =
                    "Test Suburb",

                City =
                    "Gqeberha",

                Province =
                    "Eastern Cape",

                PostalCode =
                    "6001",

                DateOfBirth =
                    new DateOnly(
                        1990,
                        1,
                        1
                    ),

                Gender =
                    "Female",

                EmploymentDate =
                    DateTime.UtcNow
                        .AddMonths(
                            -6
                        ),

                EmergencyContactName =
                    "Emergency Contact",

                EmergencyContactPhone =
                    "0711111111",

                EmergencyContactRelationship =
                    "Family"
            };
        }

        private static RegisterProxyDto
            ProxyRegistration(
                Guid clinicId,
                string fullName,
                string idNumber,
                string phone
            )
        {
            return new RegisterProxyDto
            {
                FullName =
                    fullName,

                IdNumber =
                    idNumber,

                PhoneNumber =
                    phone,

                Email =
                    $"{phone}@philalink.test",

                ClinicId =
                    clinicId,

                AddressLine1 =
                    "20 Test Street",

                AddressLine2 =
                    null,

                Suburb =
                    "Test Suburb",

                City =
                    "Gqeberha",

                Province =
                    "Eastern Cape",

                PostalCode =
                    "6001",

                Gender =
                    "Male",

                EmergencyContactName =
                    "Emergency Contact",

                EmergencyContactPhone =
                    "0711111111",

                EmergencyContactRelationship =
                    "Family"
            };
        }
    }
}
