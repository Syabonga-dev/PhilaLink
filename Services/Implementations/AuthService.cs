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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PersonalProject.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly PhilaLinkDbContext _context;
        private readonly IConfiguration _config;

        private static readonly HttpClient GoogleHttpClient =
            new HttpClient();

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
                    user =>
                        user.IdNumber == idNumber ||
                        user.PhoneNumber == phoneNumber
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

            /*
             * Login is read-only until authentication succeeds;
             * the user entity does not need EF change tracking.
             */
            var user =
                await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item =>
                            item.IdNumber ==
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
        // GOOGLE OAUTH AUTHORIZATION URL
        // =====================================================

        public string GetGoogleAuthorizationUrl(
            string state
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    state
                )
            )
            {
                throw new InvalidOperationException(
                    "Google OAuth state is required."
                );
            }

            var clientId =
                GetGoogleConfiguration(
                    "ClientId"
                );

            var redirectUri =
                GetGoogleConfiguration(
                    "RedirectUri"
                );

            var scope =
                "openid email profile";

            return
                "https://accounts.google.com/o/oauth2/v2/auth" +
                "?client_id=" +
                Uri.EscapeDataString(
                    clientId
                ) +
                "&redirect_uri=" +
                Uri.EscapeDataString(
                    redirectUri
                ) +
                "&response_type=code" +
                "&scope=" +
                Uri.EscapeDataString(
                    scope
                ) +
                "&state=" +
                Uri.EscapeDataString(
                    state
                ) +
                "&prompt=select_account";
        }

        // =====================================================
        // GOOGLE LOGIN
        // =====================================================

        public async Task<LoginResponseDto> GoogleLoginAsync(
            string authorizationCode
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    authorizationCode
                )
            )
            {
                throw new UnauthorizedAccessException(
                    "Google authorization code is missing."
                );
            }

            var clientId =
                GetGoogleConfiguration(
                    "ClientId"
                );

            var clientSecret =
                GetGoogleConfiguration(
                    "ClientSecret"
                );

            var redirectUri =
                GetGoogleConfiguration(
                    "RedirectUri"
                );

            using var tokenRequest =
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        {
                            "code",
                            authorizationCode
                        },

                        {
                            "client_id",
                            clientId
                        },

                        {
                            "client_secret",
                            clientSecret
                        },

                        {
                            "redirect_uri",
                            redirectUri
                        },

                        {
                            "grant_type",
                            "authorization_code"
                        }
                    }
                );

            HttpResponseMessage tokenResponse;

            try
            {
                tokenResponse =
                    await GoogleHttpClient.PostAsync(
                        "https://oauth2.googleapis.com/token",
                        tokenRequest
                    );
            }
            catch (
                HttpRequestException
            )
            {
                throw new InvalidOperationException(
                    "Google authentication service could not be reached."
                );
            }

            using (tokenResponse)
            {
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    throw new UnauthorizedAccessException(
                        "Google authorization could not be completed."
                    );
                }

                var responseJson =
                    await tokenResponse.Content
                        .ReadAsStringAsync();

                var googleTokens =
                    JsonSerializer.Deserialize
                        <GoogleTokenResponse>(
                            responseJson
                        );

                if (
                    googleTokens == null ||
                    string.IsNullOrWhiteSpace(
                        googleTokens.IdToken
                    )
                )
                {
                    throw new UnauthorizedAccessException(
                        "Google did not return a valid identity token."
                    );
                }

                var googlePrincipal =
                    await ValidateGoogleIdTokenAsync(
                        googleTokens.IdToken,
                        clientId
                    );

                var email =
                    googlePrincipal
                        .FindFirst("email")
                        ?.Value
                    ??
                    googlePrincipal
                        .FindFirst(
                            ClaimTypes.Email
                        )
                        ?.Value;

                var emailVerified =
                    googlePrincipal
                        .FindFirst(
                            "email_verified"
                        )
                        ?.Value;

                if (
                    string.IsNullOrWhiteSpace(
                        email
                    )
                )
                {
                    throw new UnauthorizedAccessException(
                        "Google account email was not provided."
                    );
                }

                if (
                    !string.Equals(
                        emailVerified,
                        "true",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    throw new UnauthorizedAccessException(
                        "The Google account email is not verified."
                    );
                }

                var normalizedEmail =
                    email
                        .Trim()
                        .ToLowerInvariant();

                /*
                 * Google OAuth does not create a new PhilaLink
                 * account here because a PhilaLink user requires
                 * information Google cannot provide, such as
                 * ID number and phone number.
                 *
                 * Instead, Google authenticates an existing
                 * PhilaLink account with the same verified email.
                 */
                var user =
                    await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            item =>
                                item.Email
                                    .ToLower() ==
                                normalizedEmail
                        );

                if (user == null)
                {
                    throw new UnauthorizedAccessException(
                        "No PhilaLink account is linked to this Google email. Register with PhilaLink first."
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

                if (
                    !RoleNames.IsValid(
                        user.Role
                    )
                )
                {
                    throw new UnauthorizedAccessException(
                        "This account has an unsupported role. Contact an administrator."
                    );
                }

                return CreateLoginResponse(
                    user
                );
            }
        }

        // =====================================================
        // GOOGLE TOKEN VALIDATION
        // =====================================================

        private async Task<ClaimsPrincipal>
            ValidateGoogleIdTokenAsync(
                string idToken,
                string clientId
            )
        {
            string googleKeysJson;

            try
            {
                googleKeysJson =
                    await GoogleHttpClient
                        .GetStringAsync(
                            "https://www.googleapis.com/oauth2/v3/certs"
                        );
            }
            catch (
                HttpRequestException
            )
            {
                throw new InvalidOperationException(
                    "Google signing keys could not be retrieved."
                );
            }

            var googleKeys =
                new JsonWebKeySet(
                    googleKeysJson
                );

            var validationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey =
                        true,

                    IssuerSigningKeys =
                        googleKeys.GetSigningKeys(),

                    ValidateIssuer =
                        true,

                    ValidIssuers =
                        new[]
                        {
                            "https://accounts.google.com",
                            "accounts.google.com"
                        },

                    ValidateAudience =
                        true,

                    ValidAudience =
                        clientId,

                    ValidateLifetime =
                        true,

                    RequireExpirationTime =
                        true,

                    ClockSkew =
                        TimeSpan.FromMinutes(2)
                };

            var tokenHandler =
                new JwtSecurityTokenHandler();

            try
            {
                var principal =
                    tokenHandler.ValidateToken(
                        idToken,
                        validationParameters,
                        out var validatedToken
                    );

                if (
                    validatedToken
                        is not JwtSecurityToken jwtToken
                )
                {
                    throw new UnauthorizedAccessException(
                        "Google identity token is invalid."
                    );
                }

                if (
                    !string.Equals(
                        jwtToken.Header.Alg,
                        SecurityAlgorithms.RsaSha256,
                        StringComparison.Ordinal
                    )
                )
                {
                    throw new UnauthorizedAccessException(
                        "Google identity token uses an unsupported signing algorithm."
                    );
                }

                return principal;
            }
            catch (
                SecurityTokenException
            )
            {
                throw new UnauthorizedAccessException(
                    "Google identity token validation failed."
                );
            }
        }

        // =====================================================
        // GOOGLE CONFIGURATION
        // =====================================================

        private string GetGoogleConfiguration(
            string name
        )
        {
            var value =
                _config[
                    $"GoogleOAuth:{name}"
                ];

            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                throw new InvalidOperationException(
                    $"Google OAuth configuration is missing: GoogleOAuth:{name}."
                );
            }

            return value;
        }

        // =====================================================
        // AUTHENTICATED USER
        // =====================================================

        public async Task<UserResponseDto> GetMeAsync(
            Guid userId
        )
        {
            /*
             * /api/auth/me is called during frontend bootstrap.
             * Project only the fields the API returns and avoid
             * change tracking for this read-only request.
             */
            var user =
                await _context.Users
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.Id ==
                                userId
                    )
                    .Select(
                        item =>
                            new UserResponseDto
                            {
                                Id =
                                    item.Id,

                                FullName =
                                    item.FullName,

                                IdNumber =
                                    item.IdNumber,

                                PhoneNumber =
                                    item.PhoneNumber,

                                Email =
                                    item.Email,

                                Role =
                                    item.Role,

                                IsActive =
                                    item.IsActive,

                                IsVerified =
                                    item.IsVerified,

                                MustChangePassword =
                                    item.MustChangePassword
                            }
                    )
                    .FirstOrDefaultAsync();

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

            return user;
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
            /*
             * Tracking is required because PasswordHash,
             * MustChangePassword and UpdatedAt are modified.
             */
            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
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
                    patient =>
                        patient.PatientNumber ==
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
                    character =>
                        !char.IsLetterOrDigit(
                            character
                        )
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

        // =====================================================
        // GOOGLE TOKEN RESPONSE
        // =====================================================

        private sealed class GoogleTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken
            {
                get;
                set;
            }

            [JsonPropertyName("id_token")]
            public string? IdToken
            {
                get;
                set;
            }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn
            {
                get;
                set;
            }

            [JsonPropertyName("token_type")]
            public string? TokenType
            {
                get;
                set;
            }

            [JsonPropertyName("scope")]
            public string? Scope
            {
                get;
                set;
            }
        }
    }
}
