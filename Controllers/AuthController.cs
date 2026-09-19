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
        private readonly IConfiguration _config;

        public AuthController(
            IAuthService authService,
            IOtpVerificationService otpService,
            IConfiguration config
        )
        {
            _authService =
                authService;

            _otpService =
                otpService;

            _config =
                config;
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
                            TimeSpan.FromMinutes(10),

                        Path =
                            "/"
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
                DeleteGoogleStateCookie();

                return RedirectToGoogleError(
                    "Google authentication was cancelled or failed."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    code
                )
            )
            {
                DeleteGoogleStateCookie();

                return RedirectToGoogleError(
                    "Google authorization code was not provided."
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
                DeleteGoogleStateCookie();

                return RedirectToGoogleError(
                    "Google authentication state validation failed."
                );
            }

            DeleteGoogleStateCookie();

            try
            {
                var result =
                    await _authService
                        .GoogleLoginAsync(
                            code
                        );

                var frontendBaseUrl =
                    GetFrontendBaseUrl();

                var redirectUrl =
                    frontendBaseUrl +
                    "/auth/google/callback" +
                    "#token=" +
                    Uri.EscapeDataString(
                        result.Token
                    );

                return Redirect(
                    redirectUrl
                );
            }
            catch (
                UnauthorizedAccessException ex
            )
            {
                return RedirectToGoogleError(
                    ex.Message
                );
            }
            catch (
                InvalidOperationException
            )
            {
                return RedirectToGoogleError(
                    "Google sign-in could not be completed. Please try again."
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
        // GOOGLE OAUTH HELPERS
        // =====================================================

        private string GetFrontendBaseUrl()
        {
            var frontendBaseUrl =
                _config[
                    "Frontend:BaseUrl"
                ];

            if (
                string.IsNullOrWhiteSpace(
                    frontendBaseUrl
                )
            )
            {
                throw new InvalidOperationException(
                    "Frontend:BaseUrl is missing from configuration."
                );
            }

            return frontendBaseUrl
                .TrimEnd('/');
        }

        private IActionResult
            RedirectToGoogleError(
                string message
            )
        {
            var frontendBaseUrl =
                GetFrontendBaseUrl();

            var redirectUrl =
                frontendBaseUrl +
                "/auth/google/callback" +
                "#error=" +
                Uri.EscapeDataString(
                    message
                );

            return Redirect(
                redirectUrl
            );
        }

        private void
            DeleteGoogleStateCookie()
        {
            Response.Cookies.Delete(
                GoogleStateCookie,
                new CookieOptions
                {
                    Path =
                        "/",

                    Secure =
                        true,

                    SameSite =
                        SameSiteMode.Lax
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
