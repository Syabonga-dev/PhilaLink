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
        private readonly PhilaLinkDbContext _context;
        private readonly IOtpVerificationService _otpService;

        public PasswordResetController(
            PhilaLinkDbContext context,
            IOtpVerificationService otpService
        )
        {
            _context = context;
            _otpService = otpService;
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

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.Email == identifier ||
                            u.IdNumber == identifier
                    );

            /*
             * Always return the same message whether or not
             * the account exists. This prevents account
             * enumeration.
             */
            if (user == null)
            {
                return Ok(
                    new
                    {
                        message =
                            "If an account matches those details, a verification code has been sent."
                    }
                );
            }

            try
            {
                await _otpService.GenerateAsync(
                    user.Id,
                    "PasswordReset"
                );
            }
            catch (InvalidOperationException)
            {
                /*
                 * Do not expose whether an account exists
                 * or whether email delivery failed.
                 */
            }

            return Ok(
                new
                {
                    message =
                        "If an account matches those details, a verification code has been sent."
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

            var identifier =
                dto.Identifier.Trim();

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.Email == identifier ||
                            u.IdNumber == identifier
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
                await _otpService.VerifyAsync(
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

            if (
                dto.NewPassword.Length <
                12
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Password must contain at least 12 characters."
                    }
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

            return Ok(
                new
                {
                    message =
                        "Password reset successfully."
                }
            );
        }
    }
}
