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
            _context = context;

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
                identifier.ToLower();

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.IsActive &&
                            (
                                u.IdNumber ==
                                    identifier ||
                                u.Email.ToLower() ==
                                    normalizedIdentifier
                            )
                    );

            /*
             * Never reveal whether the supplied email or
             * ID number belongs to a PhilaLink account.
             */
            if (user == null)
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
                        user.Id,
                        "PasswordReset"
                    );
            }
            catch (InvalidOperationException)
            {
                /*
                 * Keep the public response identical even
                 * when delivery fails or resend cooldown
                 * is active.
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
             * Validate the password before consuming the OTP.
             * A valid OTP should not be wasted because the
             * chosen password failed the password policy.
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
                identifier.ToLower();

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.IsActive &&
                            (
                                u.IdNumber ==
                                    identifier ||
                                u.Email.ToLower() ==
                                    normalizedIdentifier
                            )
                    );

            if (user == null)
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
                BCrypt.Net.BCrypt.HashPassword(
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
                password.Length < 12
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
