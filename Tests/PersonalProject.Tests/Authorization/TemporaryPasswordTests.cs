using Microsoft.Extensions.Configuration;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Implementations;
using PersonalProject.Tests.Support;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace PersonalProject.Tests.Authorization
{
    public class TemporaryPasswordTests
    {
        private const string
            TemporaryPassword =
                "TempPass!12345";

        private const string
            NewPassword =
                "NewSecure!12345";

        [Fact]
        public async Task
            TemporaryPasswordLoginProducesRestrictedJwt()
        {
            await using var db =
                TestDb.Create();

            var user =
                TestDataFactory.User(
                    RoleNames.Nurse,
                    "Temporary Nurse",
                    "301"
                );

            user.PasswordHash =
                BCrypt.Net.BCrypt
                    .HashPassword(
                        TemporaryPassword
                    );

            user.MustChangePassword =
                true;

            db.Users.Add(
                user
            );

            await db.SaveChangesAsync();

            var service =
                CreateService(
                    db
                );

            var result =
                await service
                    .LoginAsync(
                        new LoginDto
                        {
                            IdNumber =
                                user.IdNumber,

                            Password =
                                TemporaryPassword
                        }
                    );

            Assert.True(
                result.User
                    .MustChangePassword
            );

            var token =
                new JwtSecurityTokenHandler()
                    .ReadJwtToken(
                        result.Token
                    );

            Assert.Equal(
                "true",
                token.Claims
                    .Single(
                        claim =>
                            claim.Type ==
                            "mustChangePassword"
                    )
                    .Value
            );
        }

        [Fact]
        public async Task
            PasswordChangeClearsRestrictionAndInvalidatesOldPassword()
        {
            await using var db =
                TestDb.Create();

            var user =
                TestDataFactory.User(
                    RoleNames.Nurse,
                    "Temporary Nurse",
                    "302"
                );

            user.PasswordHash =
                BCrypt.Net.BCrypt
                    .HashPassword(
                        TemporaryPassword
                    );

            user.MustChangePassword =
                true;

            db.Users.Add(
                user
            );

            await db.SaveChangesAsync();

            var service =
                CreateService(
                    db
                );

            var result =
                await service
                    .ChangePasswordAsync(
                        user.Id,
                        new ChangePasswordDto
                        {
                            CurrentPassword =
                                TemporaryPassword,

                            NewPassword =
                                NewPassword,

                            ConfirmNewPassword =
                                NewPassword
                        }
                    );

            Assert.False(
                result.User
                    .MustChangePassword
            );

            var storedUser =
                await db.Users
                    .FindAsync(
                        user.Id
                    );

            Assert.NotNull(
                storedUser
            );

            Assert.False(
                storedUser!
                    .MustChangePassword
            );

            Assert.True(
                BCrypt.Net.BCrypt
                    .Verify(
                        NewPassword,
                        storedUser
                            .PasswordHash
                    )
            );

            Assert.False(
                BCrypt.Net.BCrypt
                    .Verify(
                        TemporaryPassword,
                        storedUser
                            .PasswordHash
                    )
            );

            var token =
                new JwtSecurityTokenHandler()
                    .ReadJwtToken(
                        result.Token
                    );

            Assert.Equal(
                "false",
                token.Claims
                    .Single(
                        claim =>
                            claim.Type ==
                            "mustChangePassword"
                    )
                    .Value
            );
        }

        [Fact]
        public async Task
            NewPasswordCannotEqualTemporaryPassword()
        {
            await using var db =
                TestDb.Create();

            var user =
                TestDataFactory.User(
                    RoleNames.Nurse,
                    "Temporary Nurse",
                    "303"
                );

            user.PasswordHash =
                BCrypt.Net.BCrypt
                    .HashPassword(
                        TemporaryPassword
                    );

            user.MustChangePassword =
                true;

            db.Users.Add(
                user
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
                        .ChangePasswordAsync(
                            user.Id,
                            new ChangePasswordDto
                            {
                                CurrentPassword =
                                    TemporaryPassword,

                                NewPassword =
                                    TemporaryPassword,

                                ConfirmNewPassword =
                                    TemporaryPassword
                            }
                        )
            );
        }

        private static AuthService
            CreateService(
                PersonalProject.Data
                    .PhilaLinkDbContext db
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
                                "PhilaLink-Test-Key-Only-For-Automated-Authorization-Tests-2026",

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
