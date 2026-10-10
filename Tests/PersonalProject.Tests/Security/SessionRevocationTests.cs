using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Security;
using PersonalProject.Services.Implementations;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace PersonalProject.Tests.Security
{
    public class SessionRevocationTests
    {
        [Fact]
        public async Task
            SecuritySensitiveUserChangeAdvancesTokenVersion()
        {
            await using var db =
                CreateDb();

            var user =
                CreateUser();

            db.Users.Add(
                user
            );

            await db
                .SaveChangesAsync();

            Assert.Equal(
                1,
                user.TokenVersion
            );

            user.PasswordHash =
                BCrypt.Net.BCrypt
                    .HashPassword(
                        "ChangedPass!12345"
                    );

            await db
                .SaveChangesAsync();

            Assert.Equal(
                2,
                user.TokenVersion
            );

            user.IsActive =
                false;

            await db
                .SaveChangesAsync();

            Assert.Equal(
                3,
                user.TokenVersion
            );
        }

        [Fact]
        public async Task
            ExplicitSessionRevocationAdvancesTokenVersion()
        {
            await using var db =
                CreateDb();

            var user =
                CreateUser();

            db.Users.Add(
                user
            );

            await db
                .SaveChangesAsync();

            var service =
                new SessionService(
                    db
                );

            await service
                .RevokeAllSessionsAsync(
                    user.Id
                );

            Assert.Equal(
                2,
                user.TokenVersion
            );
        }

        [Fact]
        public async Task
            LoginTokenContainsCurrentTokenVersion()
        {
            await using var db =
                CreateDb();

            const string password =
                "ValidPass!12345";

            var user =
                CreateUser();

            user.PasswordHash =
                BCrypt.Net.BCrypt
                    .HashPassword(
                        password
                    );

            db.Users.Add(
                user
            );

            await db
                .SaveChangesAsync();

            var configuration =
                CreateConfiguration();

            var inner =
                new AuthService(
                    db,
                    configuration
                );

            var service =
                new SessionAwareAuthService(
                    inner,
                    db,
                    configuration
                );

            var response =
                await service.LoginAsync(
                    new LoginDto
                    {
                        IdNumber =
                            user.IdNumber,

                        Password =
                            password
                    }
                );

            var token =
                new JwtSecurityTokenHandler()
                    .ReadJwtToken(
                        response.Token
                    );

            Assert.Equal(
                "1",
                token.Claims
                    .First(
                        claim =>
                            claim.Type ==
                            "tokenVersion"
                    )
                    .Value
            );
        }

        private static
            PhilaLinkDbContext
            CreateDb()
        {
            var options =
                new DbContextOptionsBuilder<
                    PhilaLinkDbContext
                >()
                .UseInMemoryDatabase(
                    $"session-tests-{Guid.NewGuid():N}"
                )
                .AddInterceptors(
                    new TokenVersionSaveChangesInterceptor()
                )
                .Options;

            var db =
                new PhilaLinkDbContext(
                    options
                );

            db.Database
                .EnsureCreated();

            return db;
        }

        private static User
            CreateUser()
        {
            return new User
            {
                Id =
                    Guid.NewGuid(),

                FullName =
                    "Session Test Patient",

                IdNumber =
                    $"900101{Random.Shared.Next(1000000, 9999999)}",

                PhoneNumber =
                    $"+2782{Random.Shared.Next(1000000, 9999999)}",

                Email =
                    $"{Guid.NewGuid():N}@philalink.test",

                PasswordHash =
                    BCrypt.Net.BCrypt
                        .HashPassword(
                            "ValidPass!12345"
                        ),

                Role =
                    RoleNames.Patient,

                IsActive =
                    true,

                IsVerified =
                    true,

                VerifiedAt =
                    DateTime.UtcNow,

                MustChangePassword =
                    false,

                TokenVersion =
                    1,

                CreatedAt =
                    DateTime.UtcNow
            };
        }

        private static IConfiguration
            CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<
                        string,
                        string?
                    >
                    {
                        [
                            "Jwt:Key"
                        ] =
                            "PhilaLink-Test-Key-That-Is-Long-Enough-For-HmacSha256-2026",

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
        }
    }
}