using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/auth/password-reset")]
    public class PasswordResetController : ControllerBase
    {
        private const string GenericResetMessage =
            "If an account matches those details, a verification code has been sent.";

        private readonly PhilaLinkDbContext _context;

        private readonly IOtpVerificationService
            _otpService;

        public PasswordResetController(
            PhilaLinkDbContext context,
            IOtpVerificationService otpService
        )
        {
            _context =
                context;

            _otpService =
                otpService;
        }

        // =====================================================
        // REQUEST PASSWORD RESET OTP
        // =====================================================

        [HttpPost("request")]
        public async Task<IActionResult> RequestReset(
            ForgotPasswordRequestDto dto
        )
        {
            var identifier =
                dto.Identifier.Trim();

            var normalizedIdentifier =
                identifier
                    .ToLowerInvariant();

            /*
             * Only User.Id is required to generate the OTP.
             *
             * Do not materialize and track the entire User row.
             * Email is already canonicalized to lowercase.
             */
            var userId =
                await _context.Users
                    .AsNoTracking()
                    .Where(
                        user =>
                            user.IsActive &&
                            (
                                user.IdNumber ==
                                    identifier ||
                                user.Email ==
                                    normalizedIdentifier
                            )
                    )
                    .Select(
                        user =>
                            (Guid?)user.Id
                    )
                    .FirstOrDefaultAsync();

            /*
             * Never disclose whether the supplied identifier
             * belongs to an account.
             */
            if (
                userId ==
                null
            )
            {
                return Ok(
                    new
                    {
                        message =
                            GenericResetMessage
                    }
                );
            }

            try
            {
                await _otpService
                    .GenerateAsync(
                        userId.Value,
                        "PasswordReset"
                    );
            }
            catch (InvalidOperationException)
            {
                /*
                 * Deliberately keep the public response identical
                 * for resend cooldowns and delivery failures.
                 */
            }

            return Ok(
                new
                {
                    message =
                        GenericResetMessage
                }
            );
        }

        // =====================================================
        // RESET PASSWORD
        // =====================================================

        [HttpPost("reset")]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordDto dto
        )
        {
            if (
                dto.NewPassword !=
                dto.ConfirmNewPassword
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Passwords do not match."
                    }
                );
            }

            var passwordError =
                GetPasswordValidationError(
                    dto.NewPassword
                );

            /*
             * Validate before consuming a valid OTP.
             */
            if (
                passwordError !=
                null
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            passwordError
                    }
                );
            }

            var identifier =
                dto.Identifier.Trim();

            var normalizedIdentifier =
                identifier
                    .ToLowerInvariant();

            /*
             * Tracking is required for this query because the
             * password/security fields are updated below.
             *
             * Direct Email equality keeps the lookup indexable.
             */
            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        item =>
                            item.IsActive &&
                            (
                                item.IdNumber ==
                                    identifier ||
                                item.Email ==
                                    normalizedIdentifier
                            )
                    );

            if (
                user ==
                null
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Invalid or expired verification code."
                    }
                );
            }

            var verified =
                await _otpService
                    .VerifyAsync(
                        user.Id,
                        dto.Code,
                        "PasswordReset"
                    );

            if (!verified)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Invalid or expired verification code."
                    }
                );
            }

            user.PasswordHash =
                BCrypt.Net.BCrypt
                    .HashPassword(
                        dto.NewPassword
                    );

            user.ClearLoginAbuseState();

            user.MustChangePassword =
                false;

            user.UpdatedAt =
                DateTime.UtcNow;

            await _context
                .SaveChangesAsync();

            return Ok(
                new
                {
                    message =
                        "Password reset successfully."
                }
            );
        }

        // =====================================================
        // PASSWORD POLICY
        // =====================================================

        private static string?
            GetPasswordValidationError(
                string password
            )
        {
            if (
                string.IsNullOrWhiteSpace(
                    password
                ) ||
                password.Length <
                    12
            )
            {
                return
                    "Password must contain at least 12 characters.";
            }

            if (
                !password.Any(
                    char.IsUpper
                )
            )
            {
                return
                    "Password must contain at least one uppercase letter.";
            }

            if (
                !password.Any(
                    char.IsLower
                )
            )
            {
                return
                    "Password must contain at least one lowercase letter.";
            }

            if (
                !password.Any(
                    char.IsDigit
                )
            )
            {
                return
                    "Password must contain at least one number.";
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
                return
                    "Password must contain at least one special character.";
            }

            return null;
        }
    }
}