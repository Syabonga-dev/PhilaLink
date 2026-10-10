using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PersonalProject.Services.Implementations
{
    /*
     * AuthService continues handling the existing
     * authentication workflows.
     *
     * SessionAwareAuthService decorates AuthService and
     * replaces every returned PhilaLink JWT with a JWT
     * containing the user's current TokenVersion.
     */
    public class SessionAwareAuthService :
        IAuthService
    {
        private const int MaxFailedLoginAttempts = 5;

        private static readonly TimeSpan FailedLoginAttemptWindow =
            TimeSpan.FromMinutes(15);

        private static readonly TimeSpan LoginLockoutDuration =
            TimeSpan.FromMinutes(15);

        private static readonly string DummyPasswordHash =
            BCrypt.Net.BCrypt.HashPassword(
                "PhilaLink-Dummy-Password-Only!2026"
            );

        private readonly AuthService
            _inner;

        private readonly PhilaLinkDbContext
            _context;

        private readonly IConfiguration
            _configuration;

        public SessionAwareAuthService(
            AuthService inner,
            PhilaLinkDbContext context,
            IConfiguration configuration
        )
        {
            _inner =
                inner;

            _context =
                context;

            _configuration =
                configuration;
        }

        // =====================================================
        // REGISTRATION
        // =====================================================

        public Task<RegisterResponseDto>
            RegisterAsync(
                RegisterDto dto
            )
        {
            return _inner.RegisterAsync(
                dto
            );
        }

        // =====================================================
        // LOGIN
        // =====================================================

        public async Task<LoginResponseDto>
            LoginAsync(
                LoginDto dto
            )
        {
            var idNumber = dto.IdNumber.Trim();
            var user = await _context.Users.FirstOrDefaultAsync(
                item => item.IdNumber == idNumber
            );

            var passwordValid = BCrypt.Net.BCrypt.Verify(
                dto.Password,
                user?.PasswordHash ?? DummyPasswordHash
            );

            var now = DateTime.UtcNow;

            if (user == null)
            {
                throw new UnauthorizedAccessException(
                    "Invalid ID number or password."
                );
            }

            if (!passwordValid)
            {
                await RecordFailedLoginAsync(user, now);

                throw new UnauthorizedAccessException(
                    "Invalid ID number or password."
                );
            }

            if (user.LockoutEndUtc > now)
            {
                throw new UnauthorizedAccessException(
                    "Too many failed sign-in attempts. Please wait before trying again or reset your password."
                );
            }

            if (user.FailedLoginAttempts != 0 ||
                user.LastFailedLoginAtUtc.HasValue ||
                user.LockoutEndUtc.HasValue)
            {
                user.ClearLoginAbuseState();
                await _context.SaveChangesAsync();
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive. Contact an administrator."
                );
            }

            if (user.Role == RoleNames.Patient && !user.IsVerified)
            {
                throw new UnauthorizedAccessException(
                    "Account verification is required before login."
                );
            }

            if (!RoleNames.IsValid(user.Role))
            {
                throw new UnauthorizedAccessException(
                    "This account has an unsupported role. Contact an administrator."
                );
            }

            return await
                ReplaceTokenAsync(
                    new LoginResponseDto
                    {
                        User = CreateUserResponse(user)
                    }
                );
        }

        private async Task RecordFailedLoginAsync(
            User user,
            DateTime now
        )
        {
            if (user.LockoutEndUtc > now)
            {
                return;
            }

            if (user.LockoutEndUtc.HasValue &&
                user.LockoutEndUtc.Value <= now)
            {
                user.ClearLoginAbuseState();
            }

            if (!user.LastFailedLoginAtUtc.HasValue ||
                now - user.LastFailedLoginAtUtc.Value >
                    FailedLoginAttemptWindow)
            {
                user.FailedLoginAttempts = 0;
            }

            user.FailedLoginAttempts =
                Math.Max(0, user.FailedLoginAttempts) + 1;
            user.LastFailedLoginAtUtc = now;

            if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
            {
                user.LockoutEndUtc = now.Add(LoginLockoutDuration);
            }

            await _context.SaveChangesAsync();
        }

        // =====================================================
        // CURRENT USER
        // =====================================================

        public Task<UserResponseDto>
            GetMeAsync(
                Guid userId
            )
        {
            return _inner.GetMeAsync(
                userId
            );
        }

        // =====================================================
        // CHANGE PASSWORD
        // =====================================================

        public async Task<LoginResponseDto>
            ChangePasswordAsync(
                Guid userId,
                ChangePasswordDto dto
            )
        {
            var response =
                await _inner
                    .ChangePasswordAsync(
                        userId,
                        dto
                    );

            /*
             * ChangePasswordAsync saves the new password.
             * The interceptor increments TokenVersion.
             *
             * We therefore issue the replacement JWT using
             * the new current TokenVersion.
             */
            return await
                ReplaceTokenAsync(
                    response
                );
        }

        // =====================================================
        // GOOGLE OAUTH
        // =====================================================

        public string
            GetGoogleAuthorizationUrl(
                string state
            )
        {
            return _inner
                .GetGoogleAuthorizationUrl(
                    state
                );
        }

        public async Task<LoginResponseDto>
            GoogleLoginAsync(
                string authorizationCode
            )
        {
            var response =
                await _inner
                    .GoogleLoginAsync(
                        authorizationCode
                    );

            return await
                ReplaceTokenAsync(
                    response
                );
        }

        // =====================================================
        // TOKEN REPLACEMENT
        // =====================================================

        private async Task<LoginResponseDto>
            ReplaceTokenAsync(
                LoginResponseDto response
            )
        {
            var user =
                await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            response.User.Id
                    );

            if (
                user == null ||
                !user.IsActive
            )
            {
                throw new UnauthorizedAccessException(
                    "Account is not available."
                );
            }

            return new LoginResponseDto
            {
                Token =
                    GenerateToken(
                        user
                    ),

                User =
                    response.User
            };
        }

        private static UserResponseDto CreateUserResponse(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                IdNumber = user.IdNumber,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                IsVerified = user.IsVerified,
                MustChangePassword = user.MustChangePassword
            };
        }

        // =====================================================
        // SESSION-AWARE JWT
        // =====================================================

        private string GenerateToken(
            User user
        )
        {
            var jwtKey =
                _configuration[
                    "Jwt:Key"
                ];

            if (
                string.IsNullOrWhiteSpace(
                    jwtKey
                )
            )
            {
                throw new InvalidOperationException(
                    "JWT Key is missing in configuration (Jwt:Key)."
                );
            }

            var claims =
                new[]
                {
                    new Claim(
                        ClaimTypes
                            .NameIdentifier,
                        user.Id.ToString()
                    ),

                    new Claim(
                        ClaimTypes.Name,
                        user.FullName
                    ),

                    new Claim(
                        ClaimTypes.Role,
                        user.Role
                    ),

                    new Claim(
                        "idNumber",
                        user.IdNumber
                    ),

                    new Claim(
                        "mustChangePassword",
                        user.MustChangePassword
                            ? "true"
                            : "false"
                    ),

                    /*
                     * The critical revocation claim.
                     */
                    new Claim(
                        "tokenVersion",
                        user.TokenVersion
                            .ToString()
                    ),

                    /*
                     * Unique identifier for this individual
                     * JWT. This also prepares us for future
                     * per-device session management.
                     */
                    new Claim(
                        JwtRegisteredClaimNames.Jti,
                        Guid.NewGuid()
                            .ToString("N")
                    )
                };

            var token =
                new JwtSecurityToken(
                    issuer:
                        _configuration[
                            "Jwt:Issuer"
                        ],

                    audience:
                        _configuration[
                            "Jwt:Audience"
                        ],

                    claims:
                        claims,

                    expires:
                        DateTime.UtcNow
                            .AddHours(1),

                    signingCredentials:
                        new SigningCredentials(
                            new SymmetricSecurityKey(
                                Encoding.UTF8
                                    .GetBytes(
                                        jwtKey
                                    )
                            ),
                            SecurityAlgorithms
                                .HmacSha256
                        )
                );

            return
                new JwtSecurityTokenHandler()
                    .WriteToken(
                        token
                    );
        }
    }
}