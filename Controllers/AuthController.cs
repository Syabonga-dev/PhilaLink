using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private const string GoogleStateCookie =
            "philalink_google_oauth_state";

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
        // GOOGLE OAUTH LOGIN
        // =====================================================

        [HttpGet("google-login")]
        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            try
            {
                var state =
                    Convert.ToHexString(
                        RandomNumberGenerator
                            .GetBytes(32)
                    );

                Response.Cookies.Append(
                    GoogleStateCookie,
                    state,
                    new CookieOptions
                    {
                        HttpOnly =
                            true,

                        Secure =
                            true,

                        SameSite =
                            SameSiteMode.Lax,

                        IsEssential =
                            true,

                        MaxAge =
                            TimeSpan.FromMinutes(10)
                    }
                );

                var authorizationUrl =
                    _authService
                        .GetGoogleAuthorizationUrl(
                            state
                        );

                return Redirect(
                    authorizationUrl
                );
            }
            catch (
                InvalidOperationException ex
            )
            {
                return StatusCode(
                    StatusCodes
                        .Status500InternalServerError,
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
        }

        // =====================================================
        // GOOGLE OAUTH CALLBACK
        // =====================================================

        [HttpGet("google-callback")]
        [AllowAnonymous]
        public async Task<IActionResult>
            GoogleCallback(
                [FromQuery] string? code,
                [FromQuery] string? state,
                [FromQuery] string? error
            )
        {
            if (
                !string.IsNullOrWhiteSpace(
                    error
                )
            )
            {
                Response.Cookies.Delete(
                    GoogleStateCookie
                );

                return BadRequest(
                    new
                    {
                        message =
                            "Google authentication was cancelled or failed.",

                        error
                    }
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    code
                )
            )
            {
                Response.Cookies.Delete(
                    GoogleStateCookie
                );

                return BadRequest(
                    new
                    {
                        message =
                            "Google authorization code was not provided."
                    }
                );
            }

            var storedState =
                Request.Cookies[
                    GoogleStateCookie
                ];

            if (
                string.IsNullOrWhiteSpace(
                    state
                ) ||
                string.IsNullOrWhiteSpace(
                    storedState
                ) ||
                !string.Equals(
                    state,
                    storedState,
                    StringComparison.Ordinal
                )
            )
            {
                Response.Cookies.Delete(
                    GoogleStateCookie
                );

                return Unauthorized(
                    new
                    {
                        message =
                            "Google authentication state validation failed."
                    }
                );
            }

            Response.Cookies.Delete(
                GoogleStateCookie
            );

            try
            {
                var result =
                    await _authService
                        .GoogleLoginAsync(
                            code
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
