using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Implementations;
using PersonalProject.Tests.Support;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class AuthWorkflowTests
    {
        private const string
            Password =
                "PatientPass!123";

        [Fact]
        public async Task
            RegistrationCreatesPatientAndHashesPassword()
        {
            await using var db =
                TestDb.Create();

            var service =
                CreateService(
                    db
                );

            var dto =
                Registration(
                    "Patient One",
                    "9001015000000",
                    "0710000001",
                    "patient1@philalink.test"
                );

            var result =
                await service
                    .RegisterAsync(
                        dto
                    );

            Assert.Equal(
                RoleNames.Patient,
                result.Role
            );

            Assert.True(
                result.RequiresVerification
            );

            var user =
                await db.Users
                    .SingleAsync();

            Assert.Equal(
                dto.Email,
                user.Email
            );

            Assert.True(
                user.IsActive
            );

            Assert.False(
                user.IsVerified
            );

            Assert.False(
                user.MustChangePassword
            );

            Assert.NotEqual(
                Password,
                user.PasswordHash
            );

            Assert.True(
                BCrypt.Net.BCrypt
                    .Verify(
                        Password,
                        user.PasswordHash
                    )
            );

            var patient =
                await db.Patients
                    .SingleAsync();

            Assert.Equal(
                user.Id,
                patient.UserId
            );

            Assert.Equal(
                new DateOnly(
                    1990,
                    1,
                    1
                ),
                patient.DateOfBirth
            );

            Assert.StartsWith(
                "PHL-",
                patient.PatientNumber
            );

            var preference =
                await db
                    .PatientPreferences
                    .SingleAsync();

            Assert.Equal(
                patient.Id,
                preference.PatientId
            );

            Assert.True(
                preference
                    .MedicationReminders
            );

            Assert.True(
                preference
                    .AppointmentReminders
            );
        }

        [Fact]
        public async Task
            DuplicateEmailIsRejectedCaseInsensitively()
        {
            await using var db =
                TestDb.Create();

            var service =
                CreateService(
                    db
                );

            await service.RegisterAsync(
                Registration(
                    "Patient One",
                    "9001015000000",
                    "0710000001",
                    "duplicate@philalink.test"
                )
            );

            var duplicate =
                Registration(
                    "Patient Two",
                    "9102025000000",
                    "0710000002",
                    "DUPLICATE@PHILALINK.TEST"
                );

            await Assert.ThrowsAsync<
                InvalidOperationException
            >(
                () =>
                    service.RegisterAsync(
                        duplicate
                    )
            );
        }

        [Fact]
        public async Task
            UnverifiedPatientCannotLogin()
        {
            await using var db =
                TestDb.Create();

            var service =
                CreateService(
                    db
                );

            var dto =
                Registration(
                    "Patient One",
                    "9001015000000",
                    "0710000001",
                    "login1@philalink.test"
                );

            await service.RegisterAsync(
                dto
            );

            await Assert.ThrowsAsync<
                UnauthorizedAccessException
            >(
                () =>
                    service.LoginAsync(
                        new LoginDto
                        {
                            IdNumber =
                                dto.IdNumber,

                            Password =
                                Password
                        }
                    )
            );
        }

        [Fact]
        public async Task
            VerifiedPatientCanLogin()
        {
            await using var db =
                TestDb.Create();

            var service =
                CreateService(
                    db
                );

            var dto =
                Registration(
                    "Patient One",
                    "9001015000000",
                    "0710000001",
                    "login2@philalink.test"
                );

            await service.RegisterAsync(
                dto
            );

            var user =
                await db.Users
                    .SingleAsync();

            user.IsVerified =
                true;

            user.VerifiedAt =
                DateTime.UtcNow;

            await db.SaveChangesAsync();

            var result =
                await service.LoginAsync(
                    new LoginDto
                    {
                        IdNumber =
                            dto.IdNumber,

                        Password =
                            Password
                    }
                );

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.Token
                )
            );

            Assert.Equal(
                user.Id,
                result.User.Id
            );

            Assert.Equal(
                RoleNames.Patient,
                result.User.Role
            );
        }

        [Fact]
        public async Task
            InactiveVerifiedPatientCannotLogin()
        {
            await using var db =
                TestDb.Create();

            var service =
                CreateService(
                    db
                );

            var dto =
                Registration(
                    "Inactive Patient",
                    "9203035000000",
                    "0710000003",
                    "inactive@philalink.test"
                );

            await service.RegisterAsync(
                dto
            );

            var user =
                await db.Users
                    .SingleAsync();

            user.IsVerified =
                true;

            user.VerifiedAt =
                DateTime.UtcNow;

            user.IsActive =
                false;

            await db.SaveChangesAsync();

            var exception =
                await Assert.ThrowsAsync<
                    UnauthorizedAccessException
                >(
                    () =>
                        service.LoginAsync(
                            new LoginDto
                            {
                                IdNumber =
                                    dto.IdNumber,

                                Password =
                                    Password
                            }
                        )
                );

            Assert.Contains(
                "inactive",
                exception.Message
                    .ToLowerInvariant()
            );
        }

        [Fact]
        public async Task
            WeakRegistrationPasswordIsRejected()
        {
            await using var db =
                TestDb.Create();

            var service =
                CreateService(
                    db
                );

            var dto =
                Registration(
                    "Patient One",
                    "9001015000000",
                    "0710000001",
                    "weak@philalink.test"
                );

            dto.Password =
                "weak";

            await Assert.ThrowsAsync<
                InvalidOperationException
            >(
                () =>
                    service.RegisterAsync(
                        dto
                    )
            );
        }

        private static RegisterDto
            Registration(
                string fullName,
                string idNumber,
                string phone,
                string email
            )
        {
            return new RegisterDto
            {
                FullName =
                    fullName,

                IdNumber =
                    idNumber,

                PhoneNumber =
                    phone,

                Email =
                    email,

                Password =
                    Password
            };
        }

        private static AuthService
            CreateService(
                PhilaLinkDbContext db
            )
        {
            var configuration =
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<
                            string,
                            string?
                        >
                        {
                            [
                                "Jwt:Key"
                            ] =
                                "PhilaLink-Automated-Test-JWT-Key-2026-Long-Enough",

                            [
                                "Jwt:Issuer"
                            ] =
                                "PhilaLink.Tests",

                            [
                                "Jwt:Audience"
                            ] =
                                "PhilaLink.Tests"
                        }
                    )
                    .Build();

            return new AuthService(
                db,
                configuration
            );
        }
    }
}
