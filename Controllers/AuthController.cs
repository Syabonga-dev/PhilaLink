using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController :
        ControllerBase
    {
        private const string
            GoogleStateCookie =
                "philalink_google_oauth_state";

        private const string
            GenericVerificationRequestMessage =
                "If this verification request is valid, a verification code has been sent.";

        private readonly IAuthService
            _authService;

        private readonly ISessionService
            _sessionService;

        private readonly IOtpVerificationService
            _otpService;

        private readonly PhilaLinkDbContext
            _context;

        private readonly IConfiguration
            _config;

        private readonly ILogger<AuthController>
            _logger;

        public AuthController(
            IAuthService authService,
            ISessionService sessionService,
            IOtpVerificationService otpService,
            PhilaLinkDbContext context,
            IConfiguration config,
            ILogger<AuthController> logger
        )
        {
            _authService =
                authService;

            _sessionService =
                sessionService;

            _otpService =
                otpService;

            _context =
                context;

            _config =
                config;

            _logger =
                logger;
        }

        // =====================================================
        // REGISTER
        // =====================================================

        [HttpPost("register")]
        public async Task<IActionResult>
            Register(
                RegisterDto dto
            )
        {
            try
            {
                var result =
                    await _authService
                        .RegisterAsync(
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
        public async Task<IActionResult>
            Login(
                LoginDto dto
            )
        {
            try
            {
                var result =
                    await _authService
                        .LoginAsync(
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
        public IActionResult
            GoogleLogin()
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
                            TimeSpan.FromMinutes(
                                10
                            ),

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
                /*
                 * Keep configuration/infrastructure details
                 * in server-side logs only.
                 */
                _logger.LogError(
                    ex,
                    "Failed to start Google OAuth login."
                );

                return StatusCode(
                    StatusCodes
                        .Status500InternalServerError,
                    new
                    {
                        message =
                            "Google sign-in is temporarily unavailable. Please try again."
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
                )
                ||
                string.IsNullOrWhiteSpace(
                    storedState
                )
                ||
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
                InvalidOperationException ex
            )
            {
                _logger.LogError(
                    ex,
                    "Google OAuth callback failed."
                );

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
         * frontend can inspect MustChangePassword.
         */
        [HttpGet("me")]
        [Authorize(
            Policy =
                "PasswordChangeAllowed"
        )]
        public async Task<IActionResult>
            Me()
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
         * This endpoint intentionally permits users whose
         * MustChangePassword flag is still true.
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
        // LOGOUT / SESSION REVOCATION
        // =====================================================

        /*
         * This is no longer a browser-only logout.
         *
         * Incrementing TokenVersion invalidates every currently
         * issued PhilaLink JWT belonging to this account.
         */
        [HttpPost("logout")]
        [Authorize(
            Policy =
                "PasswordChangeAllowed"
        )]
        public async Task<IActionResult>
            Logout()
        {
            try
            {
                await _sessionService
                    .RevokeAllSessionsAsync(
                        GetCurrentUserId()
                    );

                return Ok(
                    new
                    {
                        message =
                            "Logged out successfully."
                    }
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
        // OTP
        // =====================================================

        [HttpPost("otp/generate")]
        public async Task<IActionResult>
            GenerateOtp(
                Guid userId
            )
        {
            var eligiblePatient =
                await _context.Users
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.Id == userId &&
                            user.IsActive &&
                            !user.IsVerified &&
                            user.Role == RoleNames.Patient
                    );

            if (eligiblePatient)
            {
                try
                {
                    await _otpService
                        .GenerateAsync(
                            userId,
                            "AccountVerification"
                        );
                }
                catch (
                    InvalidOperationException ex
                )
                {
                    _logger.LogInformation(
                        ex,
                        "Account verification resend was not completed for user {UserId}.",
                        userId
                    );
                }
            }

            return Ok(
                new
                {
                    message =
                        GenericVerificationRequestMessage
                }
            );
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

        private string
            GetFrontendBaseUrl()
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

        private Guid
            GetCurrentUserId()
        {
            var claim =
                User.FindFirstValue(
                    ClaimTypes
                        .NameIdentifier
                );

            if (
                string.IsNullOrWhiteSpace(
                    claim
                )
                ||
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