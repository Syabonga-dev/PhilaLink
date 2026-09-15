using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IOtpVerificationService _otpService;

        public AuthController(
            IAuthService authService,
            IOtpVerificationService otpService
        )
        {
            _authService =
                authService;

            _otpService =
                otpService;
        }

        // =====================================================
        // REGISTER
        // =====================================================

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            RegisterDto dto
        )
        {
            try
            {
                var result =
                    await _authService.RegisterAsync(
                        dto
                    );

                return Ok(
                    result
                );
            }
            catch (
                InvalidOperationException ex
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
        }

        // =====================================================
        // LOGIN
        // =====================================================

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            LoginDto dto
        )
        {
            try
            {
                var result =
                    await _authService.LoginAsync(
                        dto
                    );

                return Ok(
                    result
                );
            }
            catch (
                UnauthorizedAccessException ex
            )
            {
                return Unauthorized(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
        }

        // =====================================================
        // CURRENT USER
        // =====================================================

        /*
         * Temporary-password accounts may call /me so the
         * client can inspect MustChangePassword.
         */
        [HttpGet("me")]
        [Authorize(
            Policy =
                "PasswordChangeAllowed"
        )]
        public async Task<IActionResult> Me()
        {
            try
            {
                return Ok(
                    await _authService
                        .GetMeAsync(
                            GetCurrentUserId()
                        )
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Unauthorized();
            }
        }

        // =====================================================
        // CHANGE PASSWORD
        // =====================================================

        /*
         * This endpoint intentionally does not require
         * MustChangePassword = false.
         */
        [HttpPost("change-password")]
        [Authorize(
            Policy =
                "PasswordChangeAllowed"
        )]
        public async Task<IActionResult>
            ChangePassword(
                ChangePasswordDto dto
            )
        {
            try
            {
                var result =
                    await _authService
                        .ChangePasswordAsync(
                            GetCurrentUserId(),
                            dto
                        );

                return Ok(
                    result
                );
            }
            catch (
                UnauthorizedAccessException ex
            )
            {
                return Unauthorized(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
            catch (
                InvalidOperationException ex
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
        }

        // =====================================================
        // OTP
        // =====================================================

        [HttpPost("otp/generate")]
        public async Task<IActionResult>
            GenerateOtp(
                Guid userId
            )
        {
            try
            {
                var expiresAt =
                    await _otpService
                        .GenerateAsync(
                            userId,
                            "AccountVerification"
                        );

                return Ok(
                    new
                    {
                        message =
                            "Verification code sent.",

                        expiresAt
                    }
                );
            }
            catch (
                InvalidOperationException ex
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
        }

        [HttpPost("otp/verify")]
        public async Task<IActionResult>
            VerifyOtp(
                Guid userId,
                string code
            )
        {
            var verified =
                await _otpService
                    .VerifyAsync(
                        userId,
                        code,
                        "AccountVerification"
                    );

            if (!verified)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Invalid or expired code."
                    }
                );
            }

            return Ok(
                new
                {
                    verified =
                        true
                }
            );
        }

        // =====================================================
        // CURRENT USER ID
        // =====================================================

        private Guid GetCurrentUserId()
        {
            var claim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (
                string.IsNullOrWhiteSpace(
                    claim
                ) ||
                !Guid.TryParse(
                    claim,
                    out var userId
                )
            )
            {
                throw new UnauthorizedAccessException();
            }

            return userId;
        }
    }
}