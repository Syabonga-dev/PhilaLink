using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;

namespace PersonalProject.Services.Implementations
{
    public class OtpVerificationService :
        IOtpVerificationService
    {
        private const int ExpiryMinutes = 5;

        private const int MaxAttempts = 5;

        private const int ResendCooldownSeconds = 60;

        private readonly PhilaLinkDbContext _context;

        private readonly IConfiguration _config;

        private readonly ILogger<OtpVerificationService>
            _logger;

        public OtpVerificationService(
            PhilaLinkDbContext context,
            IConfiguration config,
            ILogger<OtpVerificationService> logger
        )
        {
            _context =
                context;

            _config =
                config;

            _logger =
                logger;
        }

        // =====================================================
        // GENERATE OTP
        // =====================================================

        public async Task<DateTime> GenerateAsync(
            Guid userId,
            string purpose
        )
        {
            purpose =
                NormalizePurpose(
                    purpose
                );

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.Id ==
                            userId
                    );

            if (
                user == null ||
                string.IsNullOrWhiteSpace(
                    user.Email
                )
            )
            {
                throw new InvalidOperationException(
                    "Unable to send verification code."
                );
            }

            var cooldownStart =
                DateTime.UtcNow
                    .AddSeconds(
                        -ResendCooldownSeconds
                    );

            var recentlyCreated =
                await _context
                    .OtpVerifications
                    .AnyAsync(
                        o =>
                            o.UserId ==
                                userId &&
                            o.Purpose ==
                                purpose &&
                            o.CreatedAt >=
                                cooldownStart
                    );

            if (
                recentlyCreated
            )
            {
                throw new InvalidOperationException(
                    "Please wait before requesting another verification code."
                );
            }

            var existingCodes =
                await _context
                    .OtpVerifications
                    .Where(
                        o =>
                            o.UserId ==
                                userId &&
                            o.Purpose ==
                                purpose &&
                            !o.IsUsed
                    )
                    .ToListAsync();

            foreach (
                var existing
                in existingCodes
            )
            {
                existing.IsUsed =
                    true;
            }

            var generatedCode =
                GenerateSecureCode();

            var expiry =
                DateTime.UtcNow
                    .AddMinutes(
                        ExpiryMinutes
                    );

            var otp =
                new OtpVerification
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        userId,

                    Purpose =
                        purpose,

                    CodeHash =
                        BCrypt.Net.BCrypt
                            .HashPassword(
                                generatedCode
                            ),

                    ExpiryTime =
                        expiry,

                    IsUsed =
                        false,

                    AttemptCount =
                        0,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context
                .OtpVerifications
                .Add(
                    otp
                );

            await _context
                .SaveChangesAsync();

            try
            {
                await SendOtpEmailAsync(
                    user.Email,
                    user.FullName,
                    generatedCode,
                    purpose
                );
            }
            catch
            {
                /*
                 * If the email fails, the OTP must not remain
                 * usable because the user never received it.
                 */
                otp.IsUsed =
                    true;

                await _context
                    .SaveChangesAsync();

                throw;
            }

            return expiry;
        }

        // =====================================================
        // VERIFY OTP
        // =====================================================

        public async Task<bool> VerifyAsync(
            Guid userId,
            string code,
            string purpose
        )
        {
            purpose =
                NormalizePurpose(
                    purpose
                );

            if (
                string.IsNullOrWhiteSpace(
                    code
                ) ||
                code.Length !=
                    6 ||
                !code.All(
                    char.IsDigit
                )
            )
            {
                return false;
            }

            var otp =
                await _context
                    .OtpVerifications
                    .Where(
                        o =>
                            o.UserId ==
                                userId &&
                            o.Purpose ==
                                purpose &&
                            !o.IsUsed
                    )
                    .OrderByDescending(
                        o =>
                            o.CreatedAt
                    )
                    .FirstOrDefaultAsync();

            if (
                otp == null
            )
            {
                return false;
            }

            if (
                otp.ExpiryTime <=
                DateTime.UtcNow
            )
            {
                otp.IsUsed =
                    true;

                await _context
                    .SaveChangesAsync();

                return false;
            }

            if (
                otp.AttemptCount >=
                MaxAttempts
            )
            {
                otp.IsUsed =
                    true;

                await _context
                    .SaveChangesAsync();

                return false;
            }

            var valid =
                BCrypt.Net.BCrypt
                    .Verify(
                        code,
                        otp.CodeHash
                    );

            if (
                !valid
            )
            {
                otp.AttemptCount++;

                if (
                    otp.AttemptCount >=
                    MaxAttempts
                )
                {
                    otp.IsUsed =
                        true;
                }

                await _context
                    .SaveChangesAsync();

                return false;
            }

            otp.IsUsed =
                true;

            /*
             * Registration OTPs verify the account.
             * Password-reset OTPs do not change account
             * verification status.
             */
            if (
                purpose ==
                "AccountVerification"
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
                    user == null
                )
                {
                    return false;
                }

                user.IsVerified =
                    true;

                user.VerifiedAt =
                    DateTime.UtcNow;

                user.UpdatedAt =
                    DateTime.UtcNow;
            }

            await _context
                .SaveChangesAsync();

            return true;
        }

        // =====================================================
        // PURPOSE
        // =====================================================

        private static string NormalizePurpose(
            string purpose
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    purpose
                )
            )
            {
                return
                    "AccountVerification";
            }

            var normalized =
                purpose.Trim();

            if (
                string.Equals(
                    normalized,
                    "PasswordReset",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return
                    "PasswordReset";
            }

            return
                "AccountVerification";
        }

        // =====================================================
        // GENERATE SECURE CODE
        // =====================================================

        private static string GenerateSecureCode()
        {
            var value =
                RandomNumberGenerator
                    .GetInt32(
                        100000,
                        1000000
                    );

            return
                value.ToString();
        }

        // =====================================================
        // SEND EMAIL
        // =====================================================

        private async Task SendOtpEmailAsync(
            string toEmail,
            string recipientName,
            string code,
            string purpose
        )
        {
            var host =
                _config[
                    "Smtp:Host"
                ];

            if (
                string.IsNullOrWhiteSpace(
                    host
                )
            )
            {
                _logger.LogError(
                    "SMTP is not configured."
                );

                throw new InvalidOperationException(
                    "Verification service is temporarily unavailable."
                );
            }

            if (
                !int.TryParse(
                    _config[
                        "Smtp:Port"
                    ],
                    out var port
                )
            )
            {
                port =
                    587;
            }

            var username =
                _config[
                    "Smtp:Username"
                ];

            var password =
                _config[
                    "Smtp:Password"
                ];

            var fromAddress =
                _config[
                    "Smtp:From"
                ];

            if (
                string.IsNullOrWhiteSpace(
                    username
                ) ||
                string.IsNullOrWhiteSpace(
                    password
                )
            )
            {
                _logger.LogError(
                    "SMTP credentials are not configured."
                );

                throw new InvalidOperationException(
                    "Verification service is temporarily unavailable."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    fromAddress
                )
            )
            {
                fromAddress =
                    username;
            }

            var enableSsl =
                !bool.TryParse(
                    _config[
                        "Smtp:EnableSsl"
                    ],
                    out var configuredSsl
                ) ||
                configuredSsl;

            using var client =
                new SmtpClient(
                    host,
                    port
                )
                {
                    EnableSsl =
                        enableSsl,

                    UseDefaultCredentials =
                        false,

                    DeliveryMethod =
                        SmtpDeliveryMethod
                            .Network,

                    Credentials =
                        new NetworkCredential(
                            username,
                            password
                        )
                };

            var isPasswordReset =
                purpose ==
                "PasswordReset";

            var subject =
                isPasswordReset
                    ? "Reset your PhilaLink password"
                    : "Verify your PhilaLink account";

            var htmlBody =
                BuildEmailHtml(
                    recipientName,
                    code,
                    isPasswordReset
                );

            using var message =
                new MailMessage
                {
                    From =
                        new MailAddress(
                            fromAddress,
                            "PhilaLink"
                        ),

                    Subject =
                        subject,

                    Body =
                        htmlBody,

                    IsBodyHtml =
                        true
                };

            message.To.Add(
                toEmail
            );

            try
            {
                await client
                    .SendMailAsync(
                        message
                    );
            }
            catch (
                Exception ex
            )
            {
                _logger.LogError(
                    ex,
                    "Failed to send PhilaLink verification email."
                );

                throw new InvalidOperationException(
                    "Failed to send verification code. Please try again."
                );
            }
        }

        // =====================================================
        // EMAIL TEMPLATE
        // =====================================================

        private string BuildEmailHtml(
            string recipientName,
            string code,
            bool isPasswordReset
        )
        {
            var safeName =
                WebUtility.HtmlEncode(
                    recipientName
                );

            var safeCode =
                WebUtility.HtmlEncode(
                    code
                );

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
                frontendBaseUrl =
                    "https://philalinkmed.vercel.app";
            }

            frontendBaseUrl =
                frontendBaseUrl
                    .TrimEnd('/');

            var logoUrl =
                frontendBaseUrl +
                "/logo2.png";

            var heading =
                isPasswordReset
                    ? "Reset your password"
                    : "Verify your email";

            var message =
                isPasswordReset
                    ? "We received a request to reset your PhilaLink password. Use the verification code below to continue."
                    : "Welcome to PhilaLink. Use the verification code below to verify your email address and finish creating your account.";

            var securityMessage =
                isPasswordReset
                    ? "If you did not request a password reset, you can safely ignore this email. Your password will remain unchanged."
                    : "If you did not create a PhilaLink account, you can safely ignore this email.";

            return $@"
<!doctype html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>{heading}</title>
</head>

<body style=""
    margin:0;
    padding:0;
    background:#f5faf9;
    font-family:Arial,Helvetica,sans-serif;
    color:#171d1c;
"">

<table
    role=""presentation""
    width=""100%""
    cellspacing=""0""
    cellpadding=""0""
    border=""0""
    style=""
        width:100%;
        background:#f5faf9;
        padding:32px 12px;
    ""
>
    <tr>
        <td align=""center"">

            <table
                role=""presentation""
                width=""100%""
                cellspacing=""0""
                cellpadding=""0""
                border=""0""
                style=""
                    width:100%;
                    max-width:560px;
                    background:#ffffff;
                    border-radius:16px;
                    overflow:hidden;
                    border:1px solid #e2ecea;
                ""
            >

                <tr>
                    <td
                        style=""
                            height:6px;
                            background:#006a6a;
                            font-size:0;
                        ""
                    >
                        &nbsp;
                    </td>
                </tr>

                <tr>
                    <td
                        align=""center""
                        style=""
                            padding:32px 28px 18px;
                        ""
                    >

                        <img
                            src=""{logoUrl}""
                            alt=""PhilaLink""
                            width=""72""
                            style=""
                                display:block;
                                width:72px;
                                max-width:72px;
                                height:auto;
                                margin:0 auto 12px;
                            ""
                        >

                        <div
                            style=""
                                font-size:24px;
                                line-height:30px;
                                font-weight:700;
                                color:#006a6a;
                            ""
                        >
                            PhilaLink
                        </div>

                    </td>
                </tr>

                <tr>
                    <td
                        style=""
                            padding:0 32px 32px;
                        ""
                    >

                        <p
                            style=""
                                margin:0 0 18px;
                                font-size:16px;
                                line-height:24px;
                                color:#3d4949;
                            ""
                        >
                            Hello {safeName},
                        </p>

                        <h1
                            style=""
                                margin:0 0 14px;
                                font-size:26px;
                                line-height:34px;
                                color:#171d1c;
                            ""
                        >
                            {heading}
                        </h1>

                        <p
                            style=""
                                margin:0 0 24px;
                                font-size:15px;
                                line-height:24px;
                                color:#3d4949;
                            ""
                        >
                            {message}
                        </p>

                        <div
                            style=""
                                margin:0 0 24px;
                                padding:20px;
                                text-align:center;
                                background:#eef8f7;
                                border:1px solid #cce7e5;
                                border-radius:12px;
                            ""
                        >

                            <div
                                style=""
                                    margin:0 0 8px;
                                    font-size:12px;
                                    line-height:18px;
                                    font-weight:700;
                                    letter-spacing:1px;
                                    text-transform:uppercase;
                                    color:#3d4949;
                                ""
                            >
                                Verification code
                            </div>

                            <div
                                style=""
                                    font-size:34px;
                                    line-height:42px;
                                    font-weight:700;
                                    letter-spacing:8px;
                                    color:#006a6a;
                                ""
                            >
                                {safeCode}
                            </div>

                        </div>

                        <p
                            style=""
                                margin:0 0 18px;
                                font-size:14px;
                                line-height:22px;
                                color:#3d4949;
                            ""
                        >
                            This code expires in
                            <strong>
                                {ExpiryMinutes} minutes
                            </strong>.
                        </p>

                        <div
                            style=""
                                margin:0 0 26px;
                                padding:14px 16px;
                                background:#f8fbfa;
                                border-left:4px solid #1fa6a6;
                                border-radius:6px;
                            ""
                        >
                            <p
                                style=""
                                    margin:0;
                                    font-size:13px;
                                    line-height:20px;
                                    color:#53615f;
                                ""
                            >
                                {securityMessage}
                            </p>
                        </div>

                        <p
                            style=""
                                margin:0;
                                font-size:14px;
                                line-height:22px;
                                color:#3d4949;
                            ""
                        >
                            PhilaLink<br>
                            <strong>
                                Your health, connected.
                            </strong>
                        </p>

                    </td>
                </tr>

                <tr>
                    <td
                        align=""center""
                        style=""
                            padding:18px 24px;
                            background:#f1f7f6;
                            border-top:1px solid #e2ecea;
                            font-size:12px;
                            line-height:18px;
                            color:#6b7775;
                        ""
                    >
                        This is an automated security email from PhilaLink.
                    </td>
                </tr>

            </table>

        </td>
    </tr>
</table>

</body>
</html>";
        }
    }
}
