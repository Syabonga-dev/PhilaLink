using BCrypt.Net;
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
    public class AuthService : IAuthService
    {
        private readonly PhilaLinkDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(
            PhilaLinkDbContext context,
            IConfiguration config
        )
        {
            _context = context;
            _config = config;
        }

        // =====================================================
        // PATIENT SELF-REGISTRATION
        // =====================================================

        public async Task<RegisterResponseDto> RegisterAsync(
            RegisterDto dto
        )
        {
            ValidatePasswordStrength(
                dto.Password
            );

            var idNumber =
                dto.IdNumber.Trim();

            var phoneNumber =
                dto.PhoneNumber.Trim();

            var exists =
                await _context.Users.AnyAsync(
                    u =>
                        u.IdNumber == idNumber ||
                        u.PhoneNumber == phoneNumber
                );

            if (exists)
            {
                throw new InvalidOperationException(
                    "An account with that ID number or phone number already exists."
                );
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            var user = new User
            {
                Id =
                    Guid.NewGuid(),

                FullName =
                    dto.FullName.Trim(),

                IdNumber =
                    idNumber,

                PhoneNumber =
                    phoneNumber,

                Email =
                    dto.Email.Trim(),

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        dto.Password
                    ),

                Role =
                    RoleNames.Patient,

                IsActive =
                    true,

                IsVerified =
                    false,

                VerifiedAt =
                    null,

                MustChangePassword =
                    false,

                CreatedAt =
                    DateTime.UtcNow
            };

            _context.Users.Add(
                user
            );

            var patient =
                new Patient
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        user.Id,

                    PatientNumber =
                        await GeneratePatientNumberAsync(),

                    Email =
                        user.Email,

                    IsProfileComplete =
                        false,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.Patients.Add(
                patient
            );

            var preference =
                new PatientPreference
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        patient.Id,

                    MedicationReminders =
                        true,

                    AppointmentReminders =
                        true,

                    ClinicNotifications =
                        true,

                    HealthUpdates =
                        false,

                    ShareHealthData =
                        true,

                    AllowChatbotProfileAccess =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.PatientPreferences.Add(
                preference
            );

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new RegisterResponseDto
            {
                UserId =
                    user.Id,

                FullName =
                    user.FullName,

                Role =
                    user.Role,

                RequiresVerification =
                    true
            };
        }

        // =====================================================
        // LOGIN
        // =====================================================

        public async Task<LoginResponseDto> LoginAsync(
            LoginDto dto
        )
        {
            var idNumber =
                dto.IdNumber.Trim();

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.IdNumber ==
                            idNumber
                    );

            if (
                user == null ||
                !BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.PasswordHash
                )
            )
            {
                throw new UnauthorizedAccessException(
                    "Invalid ID number or password."
                );
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive. Contact an administrator."
                );
            }

            if (
                user.Role ==
                    RoleNames.Patient &&
                !user.IsVerified
            )
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

            return CreateLoginResponse(
                user
            );
        }

        // =====================================================
        // AUTHENTICATED USER
        // =====================================================

        public async Task<UserResponseDto> GetMeAsync(
            Guid userId
        )
        {
            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.Id ==
                            userId
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

            if (!RoleNames.IsValid(user.Role))
            {
                throw new UnauthorizedAccessException(
                    "Account role is invalid."
                );
            }

            return CreateUserResponse(
                user
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
            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.Id ==
                            userId
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

            if (
                !BCrypt.Net.BCrypt.Verify(
                    dto.CurrentPassword,
                    user.PasswordHash
                )
            )
            {
                throw new UnauthorizedAccessException(
                    "Current password is incorrect."
                );
            }

            if (
                dto.NewPassword !=
                dto.ConfirmNewPassword
            )
            {
                throw new InvalidOperationException(
                    "New password and confirmation do not match."
                );
            }

            ValidatePasswordStrength(
                dto.NewPassword
            );

            if (
                BCrypt.Net.BCrypt.Verify(
                    dto.NewPassword,
                    user.PasswordHash
                )
            )
            {
                throw new InvalidOperationException(
                    "The new password must be different from the current password."
                );
            }

            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    dto.NewPassword
                );

            user.MustChangePassword =
                false;

            user.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            /*
             * Return a fresh JWT whose password-change claim
             * reflects the new account state.
             */
            return CreateLoginResponse(
                user
            );
        }

        // =====================================================
        // PATIENT NUMBER
        // =====================================================

        private async Task<string>
            GeneratePatientNumberAsync()
        {
            string patientNumber;

            do
            {
                var suffix =
                    Guid.NewGuid()
                        .ToString("N")[..8]
                        .ToUpperInvariant();

                patientNumber =
                    $"PHL-{DateTime.UtcNow.Year}-{suffix}";
            }
            while (
                await _context.Patients.AnyAsync(
                    p =>
                        p.PatientNumber ==
                        patientNumber
                )
            );

            return patientNumber;
        }

        // =====================================================
        // PASSWORD POLICY
        // =====================================================

        private static void ValidatePasswordStrength(
            string password
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    password
                ) ||
                password.Length < 12
            )
            {
                throw new InvalidOperationException(
                    "Password must contain at least 12 characters."
                );
            }

            if (!password.Any(char.IsUpper))
            {
                throw new InvalidOperationException(
                    "Password must contain at least one uppercase letter."
                );
            }

            if (!password.Any(char.IsLower))
            {
                throw new InvalidOperationException(
                    "Password must contain at least one lowercase letter."
                );
            }

            if (!password.Any(char.IsDigit))
            {
                throw new InvalidOperationException(
                    "Password must contain at least one number."
                );
            }

            if (
                !password.Any(
                    c =>
                        !char.IsLetterOrDigit(c)
                )
            )
            {
                throw new InvalidOperationException(
                    "Password must contain at least one special character."
                );
            }
        }

        // =====================================================
        // RESPONSES
        // =====================================================

        private LoginResponseDto CreateLoginResponse(
            User user
        )
        {
            return new LoginResponseDto
            {
                Token =
                    GenerateToken(user),

                User =
                    CreateUserResponse(user)
            };
        }

        private static UserResponseDto
            CreateUserResponse(
                User user
            )
        {
            return new UserResponseDto
            {
                Id =
                    user.Id,

                FullName =
                    user.FullName,

                IdNumber =
                    user.IdNumber,

                PhoneNumber =
                    user.PhoneNumber,

                Email =
                    user.Email,

                Role =
                    user.Role,

                IsActive =
                    user.IsActive,

                IsVerified =
                    user.IsVerified,

                MustChangePassword =
                    user.MustChangePassword
            };
        }

        // =====================================================
        // JWT
        // =====================================================

        private string GenerateToken(
            User user
        )
        {
            var jwtKey =
                _config["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "JWT Key is missing in configuration (Jwt:Key)."
                );
            }

            var key =
                Encoding.UTF8.GetBytes(
                    jwtKey
                );

            var claims =
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
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
                    )
                };

            var token =
                new JwtSecurityToken(
                    issuer:
                        _config["Jwt:Issuer"],

                    audience:
                        _config["Jwt:Audience"],

                    claims:
                        claims,

                    expires:
                        DateTime.UtcNow
                            .AddHours(1),

                    signingCredentials:
                        new SigningCredentials(
                            new SymmetricSecurityKey(
                                key
                            ),
                            SecurityAlgorithms
                                .HmacSha256
                        )
                );

            return new JwtSecurityTokenHandler()
                .WriteToken(
                    token
                );
        }
    }
}