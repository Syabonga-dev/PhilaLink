using Microsoft.AspNetCore.Mvc;
using PersonalProject.Controllers;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using PersonalProject.Tests.Support;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class PasswordResetWorkflowTests
    {
        [Fact]
        public async Task
            UnknownAccountStillReturnsGenericSuccess()
        {
            await using var db =
                TestDb.Create();

            var otp =
                new StubOtpService();

            var controller =
                new PasswordResetController(
                    db,
                    otp
                );

            var result =
                await controller.RequestReset(
                    new ForgotPasswordRequestDto
                    {
                        Identifier =
                            "unknown@philalink.test"
                    }
                );

            Assert.IsType<
                OkObjectResult
            >(
                result
            );

            Assert.Equal(
                0,
                otp.GenerateCalls
            );
        }

        [Fact]
        public async Task
            WeakPasswordDoesNotConsumeOtp()
        {
            await using var db =
                TestDb.Create();

            var user =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "601"
                );

            db.Users.Add(
                user
            );

            await db.SaveChangesAsync();

            var otp =
                new StubOtpService
                {
                    VerifyResult =
                        true
                };

            var controller =
                new PasswordResetController(
                    db,
                    otp
                );

            var result =
                await controller.ResetPassword(
                    new ResetPasswordDto
                    {
                        Identifier =
                            user.Email,

                        Code =
                            "123456",

                        NewPassword =
                            "weak",

                        ConfirmNewPassword =
                            "weak"
                    }
                );

            Assert.IsType<
                BadRequestObjectResult
            >(
                result
            );

            Assert.Equal(
                0,
                otp.VerifyCalls
            );
        }

        [Fact]
        public async Task
            ValidOtpReplacesPassword()
        {
            await using var db =
                TestDb.Create();

            const string oldPassword =
                "OldSecure!123";

            const string newPassword =
                "NewSecure!123";

            var user =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "602"
                );

            user.PasswordHash =
                BCrypt.Net.BCrypt
                    .HashPassword(
                        oldPassword
                    );

            user.MustChangePassword =
                true;

            db.Users.Add(
                user
            );

            await db.SaveChangesAsync();

            var otp =
                new StubOtpService
                {
                    VerifyResult =
                        true
                };

            var controller =
                new PasswordResetController(
                    db,
                    otp
                );

            var result =
                await controller.ResetPassword(
                    new ResetPasswordDto
                    {
                        Identifier =
                            user.Email,

                        Code =
                            "123456",

                        NewPassword =
                            newPassword,

                        ConfirmNewPassword =
                            newPassword
                    }
                );

            Assert.IsType<
                OkObjectResult
            >(
                result
            );

            Assert.Equal(
                1,
                otp.VerifyCalls
            );

            Assert.True(
                BCrypt.Net.BCrypt
                    .Verify(
                        newPassword,
                        user.PasswordHash
                    )
            );

            Assert.False(
                BCrypt.Net.BCrypt
                    .Verify(
                        oldPassword,
                        user.PasswordHash
                    )
            );

            Assert.False(
                user.MustChangePassword
            );
        }

        private sealed class
            StubOtpService :
            IOtpVerificationService
        {
            public int GenerateCalls
            {
                get;
                private set;
            }

            public int VerifyCalls
            {
                get;
                private set;
            }

            public bool VerifyResult
            {
                get;
                set;
            }

            public Task<DateTime>
                GenerateAsync(
                    Guid userId,
                    string purpose
                )
            {
                GenerateCalls++;

                return Task.FromResult(
                    DateTime.UtcNow
                        .AddMinutes(
                            5
                        )
                );
            }

            public Task<bool>
                VerifyAsync(
                    Guid userId,
                    string code,
                    string purpose
                )
            {
                VerifyCalls++;

                return Task.FromResult(
                    VerifyResult
                );
            }
        }
    }
}
