using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalProject.Models.Constants;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Implementations;
using PersonalProject.Tests.Support;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class OtpVerificationTests
    {
        [Fact]
        public async Task
            CorrectVerificationCodeVerifiesAccountAndConsumesOtp()
        {
            await using var db =
                TestDb.Create();

            var user =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "501"
                );

            user.IsVerified =
                false;

            user.VerifiedAt =
                null;

            var otp =
                Otp(
                    user,
                    "123456"
                );

            db.AddRange(
                user,
                otp
            );

            await db.SaveChangesAsync();

            var service =
                CreateService(
                    db
                );

            var valid =
                await service.VerifyAsync(
                    user.Id,
                    "123456",
                    "AccountVerification"
                );

            Assert.True(
                valid
            );

            Assert.True(
                otp.IsUsed
            );

            Assert.True(
                user.IsVerified
            );

            Assert.NotNull(
                user.VerifiedAt
            );
        }

        [Fact]
        public async Task
            FiveInvalidAttemptsDisableOtp()
        {
            await using var db =
                TestDb.Create();

            var user =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "502"
                );

            var otp =
                Otp(
                    user,
                    "123456"
                );

            db.AddRange(
                user,
                otp
            );

            await db.SaveChangesAsync();

            var service =
                CreateService(
                    db
                );

            for (
                var attempt = 0;
                attempt < 5;
                attempt++
            )
            {
                var valid =
                    await service.VerifyAsync(
                        user.Id,
                        "000000",
                        "AccountVerification"
                    );

                Assert.False(
                    valid
                );
            }

            Assert.Equal(
                5,
                otp.AttemptCount
            );

            Assert.True(
                otp.IsUsed
            );

            var sixthAttempt =
                await service.VerifyAsync(
                    user.Id,
                    "123456",
                    "AccountVerification"
                );

            Assert.False(
                sixthAttempt
            );
        }

        [Fact]
        public async Task
            ExpiredOtpIsRejectedAndConsumed()
        {
            await using var db =
                TestDb.Create();

            var user =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "503"
                );

            var otp =
                Otp(
                    user,
                    "123456"
                );

            otp.ExpiryTime =
                DateTime.UtcNow
                    .AddMinutes(
                        -1
                    );

            db.AddRange(
                user,
                otp
            );

            await db.SaveChangesAsync();

            var service =
                CreateService(
                    db
                );

            var valid =
                await service.VerifyAsync(
                    user.Id,
                    "123456",
                    "AccountVerification"
                );

            Assert.False(
                valid
            );

            Assert.True(
                otp.IsUsed
            );
        }

        private static OtpVerification
            Otp(
                User user,
                string code
            )
        {
            return new OtpVerification
            {
                Id =
                    Guid.NewGuid(),

                UserId =
                    user.Id,

                User =
                    user,

                Purpose =
                    "AccountVerification",

                CodeHash =
                    BCrypt.Net.BCrypt
                        .HashPassword(
                            code
                        ),

                ExpiryTime =
                    DateTime.UtcNow
                        .AddMinutes(
                            5
                        ),

                IsUsed =
                    false,

                AttemptCount =
                    0,

                CreatedAt =
                    DateTime.UtcNow
            };
        }

        private static OtpVerificationService
            CreateService(
                PersonalProject.Data
                    .PhilaLinkDbContext db
            )
        {
            return new OtpVerificationService(
                db,
                new ConfigurationBuilder()
                    .Build(),
                NullLogger<
                    OtpVerificationService
                >.Instance
            );
        }
    }
}
