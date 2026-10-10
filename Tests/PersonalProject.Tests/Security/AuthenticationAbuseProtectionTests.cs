using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalProject.Controllers;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Implementations;
using PersonalProject.Services.Interfaces;
using PersonalProject.Tests.Support;
using Xunit;

namespace PersonalProject.Tests.Security
{
    public class AuthenticationAbuseProtectionTests
    {
        private const string CorrectPassword = "TempPass!12345";

        [Fact]
        public async Task UnknownAndIncorrectCredentialsReturnSameMessage()
        {
            await using var db = TestDb.Create();
            var user = TestDataFactory.User(
                RoleNames.Patient,
                "Patient One",
                "701"
            );

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var knownAccountFailure = await Assert.ThrowsAsync<
                UnauthorizedAccessException
            >(
                () => service.LoginAsync(
                    new LoginDto
                    {
                        IdNumber = user.IdNumber,
                        Password = "DefinitelyWrong!123"
                    }
                )
            );

            var unknownAccountFailure = await Assert.ThrowsAsync<
                UnauthorizedAccessException
            >(
                () => service.LoginAsync(
                    new LoginDto
                    {
                        IdNumber = "9999999999999",
                        Password = "DefinitelyWrong!123"
                    }
                )
            );

            Assert.Equal(
                "Invalid ID number or password.",
                knownAccountFailure.Message
            );
            Assert.Equal(
                knownAccountFailure.Message,
                unknownAccountFailure.Message
            );
        }

        [Fact]
        public async Task FiveFailedPasswordsLockAccount()
        {
            await using var db = TestDb.Create();
            var user = TestDataFactory.User(
                RoleNames.Patient,
                "Patient Two",
                "702"
            );

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = CreateService(db);

            for (var attempt = 0; attempt < 5; attempt++)
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => service.LoginAsync(
                        new LoginDto
                        {
                            IdNumber = user.IdNumber,
                            Password = "WrongPassword!123"
                        }
                    )
                );
            }

            Assert.Equal(5, user.FailedLoginAttempts);
            Assert.NotNull(user.LastFailedLoginAtUtc);
            Assert.NotNull(user.LockoutEndUtc);
            Assert.True(user.LockoutEndUtc > DateTime.UtcNow);

            var lockedLogin = await Assert.ThrowsAsync<
                UnauthorizedAccessException
            >(
                () => service.LoginAsync(
                    new LoginDto
                    {
                        IdNumber = user.IdNumber,
                        Password = CorrectPassword
                    }
                )
            );

            Assert.Contains(
                "too many failed",
                lockedLogin.Message,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task SuccessfulLoginAfterLockoutExpiryClearsFailureState()
        {
            await using var db = TestDb.Create();
            var user = TestDataFactory.User(
                RoleNames.Patient,
                "Patient Three",
                "703"
            );

            user.FailedLoginAttempts = 5;
            user.LastFailedLoginAtUtc = DateTime.UtcNow.AddMinutes(-16);
            user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(-1);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var response = await CreateService(db).LoginAsync(
                new LoginDto
                {
                    IdNumber = user.IdNumber,
                    Password = CorrectPassword
                }
            );

            Assert.False(string.IsNullOrWhiteSpace(response.Token));
            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.Null(user.LastFailedLoginAtUtc);
            Assert.Null(user.LockoutEndUtc);
        }

        [Fact]
        public async Task PasswordResetClearsLoginLockoutState()
        {
            await using var db = TestDb.Create();
            var user = TestDataFactory.User(
                RoleNames.Patient,
                "Patient Four",
                "704"
            );

            user.FailedLoginAttempts = 5;
            user.LastFailedLoginAtUtc = DateTime.UtcNow;
            user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(15);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var otp = new SuccessfulOtpService();
            var controller = new PasswordResetController(db, otp);

            var result = await controller.ResetPassword(
                new ResetPasswordDto
                {
                    Identifier = user.Email,
                    Code = "123456",
                    NewPassword = "NewSecure!123",
                    ConfirmNewPassword = "NewSecure!123"
                }
            );

            Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.Null(user.LastFailedLoginAtUtc);
            Assert.Null(user.LockoutEndUtc);

            var login = await CreateService(db).LoginAsync(
                new LoginDto
                {
                    IdNumber = user.IdNumber,
                    Password = "NewSecure!123"
                }
            );

            Assert.False(string.IsNullOrWhiteSpace(login.Token));
        }

        [Fact]
        public async Task VerificationResendIsGenericAndOnlySendsForEligiblePatients()
        {
            await using var db = TestDb.Create();
            var eligiblePatient = TestDataFactory.User(
                RoleNames.Patient,
                "Unverified Patient",
                "705"
            );
            eligiblePatient.IsVerified = false;

            var inactivePatient = TestDataFactory.User(
                RoleNames.Patient,
                "Inactive Patient",
                "706"
            );
            inactivePatient.IsActive = false;
            inactivePatient.IsVerified = false;

            var verifiedPatient = TestDataFactory.User(
                RoleNames.Patient,
                "Verified Patient",
                "707"
            );

            var nurse = TestDataFactory.User(
                RoleNames.Nurse,
                "Nurse",
                "708"
            );
            nurse.IsVerified = false;

            db.Users.AddRange(
                eligiblePatient,
                inactivePatient,
                verifiedPatient,
                nurse
            );
            await db.SaveChangesAsync();

            var otp = new SuccessfulOtpService();
            var controller = new AuthController(
                null!,
                null!,
                otp,
                db,
                new ConfigurationBuilder().Build(),
                NullLogger<AuthController>.Instance
            );

            var eligibleResponse = Assert.IsType<
                Microsoft.AspNetCore.Mvc.OkObjectResult
            >(await controller.GenerateOtp(eligiblePatient.Id));
            var ineligibleResponse = Assert.IsType<
                Microsoft.AspNetCore.Mvc.OkObjectResult
            >(await controller.GenerateOtp(inactivePatient.Id));
            await controller.GenerateOtp(verifiedPatient.Id);
            await controller.GenerateOtp(nurse.Id);
            await controller.GenerateOtp(Guid.NewGuid());

            Assert.Equal(1, otp.GenerateCalls);
            Assert.Equal(eligibleResponse.Value, ineligibleResponse.Value);
        }

        private static SessionAwareAuthService CreateService(
            PhilaLinkDbContext db
        )
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Jwt:Key"] =
                            "PhilaLink-Automated-Test-JWT-Key-2026-Long-Enough",
                        ["Jwt:Issuer"] = "PhilaLink.Tests",
                        ["Jwt:Audience"] = "PhilaLink.Tests"
                    }
                )
                .Build();

            return new SessionAwareAuthService(
                new AuthService(db, configuration),
                db,
                configuration
            );
        }

        private sealed class SuccessfulOtpService : IOtpVerificationService
        {
            public int GenerateCalls { get; private set; }

            public Task<DateTime> GenerateAsync(Guid userId, string purpose)
            {
                GenerateCalls++;
                return Task.FromResult(DateTime.UtcNow.AddMinutes(5));
            }

            public Task<bool> VerifyAsync(
                Guid userId,
                string code,
                string purpose
            )
            {
                return Task.FromResult(true);
            }
        }
    }
}
