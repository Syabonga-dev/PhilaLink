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
            _context = context;
            _config = config;
            _logger = logger;
        }

        public async Task<DateTime> GenerateAsync(
            Guid userId,
            string purpose
        )
        {
            purpose =
                NormalizePurpose(purpose);

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.Id == userId
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
                DateTime.UtcNow.AddSeconds(
                    -ResendCooldownSeconds
                );

            var recentlyCreated =
                await _context.OtpVerifications
                    .AnyAsync(
                        o =>
                            o.UserId == userId &&
                            o.Purpose == purpose &&
                            o.CreatedAt >= cooldownStart
                    );

            if (recentlyCreated)
            {
                throw new InvalidOperationException(
                    "Please wait before requesting another verification code."
                );
            }

            var existingCodes =
                await _context.OtpVerifications
                    .Where(
                        o =>
                            o.UserId == userId &&
                            o.Purpose == purpose &&
                            !o.IsUsed
                    )
                    .ToListAsync();

            foreach (var existing in existingCodes)
            {
                existing.IsUsed = true;
            }

            var generatedCode =
                GenerateSecureCode();

            var expiry =
                DateTime.UtcNow.AddMinutes(
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
                        BCrypt.Net.BCrypt.HashPassword(
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

            _context.OtpVerifications.Add(
                otp
            );

            await _context.SaveChangesAsync();

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
                otp.IsUsed = true;

                await _context.SaveChangesAsync();

                throw;
            }

            return expiry;
        }

        public async Task<bool> VerifyAsync(
            Guid userId,
            string code,
            string purpose
        )
        {
            purpose =
                NormalizePurpose(purpose);

            if (
                string.IsNullOrWhiteSpace(code) ||
                code.Length != 6 ||
                !code.All(char.IsDigit)
            )
            {
                return false;
            }

            var otp =
                await _context.OtpVerifications
                    .Where(
                        o =>
                            o.UserId == userId &&
                            o.Purpose == purpose &&
                            !o.IsUsed
                    )
                    .OrderByDescending(
                        o => o.CreatedAt
                    )
                    .FirstOrDefaultAsync();

            if (otp == null)
            {
                return false;
            }

            if (
                otp.ExpiryTime <=
                DateTime.UtcNow
            )
            {
                otp.IsUsed = true;

                await _context.SaveChangesAsync();

                return false;
            }

            if (
                otp.AttemptCount >=
                MaxAttempts
            )
            {
                otp.IsUsed = true;

                await _context.SaveChangesAsync();

                return false;
            }

            var valid =
                BCrypt.Net.BCrypt.Verify(
                    code,
                    otp.CodeHash
                );

            if (!valid)
            {
                otp.AttemptCount++;

                if (
                    otp.AttemptCount >=
                    MaxAttempts
                )
                {
                    otp.IsUsed = true;
                }

                await _context.SaveChangesAsync();

                return false;
            }

            otp.IsUsed = true;

            if (
                purpose ==
                "AccountVerification"
            )
            {
                var user =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            u => u.Id == userId
                        );

                if (user == null)
                {
                    return false;
                }

                user.IsVerified = true;

                user.VerifiedAt =
                    DateTime.UtcNow;

                user.UpdatedAt =
                    DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return true;
        }

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
                return "AccountVerification";
            }

            return purpose.Trim();
        }

        private static string GenerateSecureCode()
        {
            var value =
                RandomNumberGenerator.GetInt32(
                    100000,
                    1000000
                );

            return value.ToString();
        }

        private async Task SendOtpEmailAsync(
            string toEmail,
            string recipientName,
            string code,
            string purpose
        )
        {
            var host =
                _config["Smtp:Host"];

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
                    _config["Smtp:Port"],
                    out var port
                )
            )
            {
                port = 587;
            }

            var username =
                _config["Smtp:Username"];

            var password =
                _config["Smtp:Password"];

            var fromAddress =
                _config["Smtp:From"];

            if (
                string.IsNullOrWhiteSpace(
                    fromAddress
                )
            )
            {
                fromAddress =
                    username;
            }

            if (
                string.IsNullOrWhiteSpace(
                    fromAddress
                )
            )
            {
                throw new InvalidOperationException(
                    "SMTP sender address is not configured."
                );
            }

            var enableSsl =
                !bool.TryParse(
                    _config["Smtp:EnableSsl"],
                    out var sslConfigured
                ) ||
                sslConfigured;

            using var client =
                new SmtpClient(
                    host,
                    port
                )
                {
                    EnableSsl =
                        enableSsl
                };

            if (
                !string.IsNullOrWhiteSpace(
                    username
                )
            )
            {
                client.Credentials =
                    new NetworkCredential(
                        username,
                        password
                    );
            }

            using var message =
                new MailMessage
                {
                    From =
                        new MailAddress(
                            fromAddress,
                            "PhilaLink"
                        ),

                    Subject =
                        "Your PhilaLink verification code",

                    Body =
                        $"Hi {recipientName},\n\n" +
                        $"Your PhilaLink verification code is: {code}\n\n" +
                        $"Purpose: {purpose}\n\n" +
                        $"This code expires in {ExpiryMinutes} minutes.\n\n" +
                        "If you did not request this code, you can ignore this message.",

                    IsBodyHtml =
                        false
                };

            message.To.Add(
                toEmail
            );

            try
            {
                await client.SendMailAsync(
                    message
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send verification email."
                );

                throw new InvalidOperationException(
                    "Failed to send verification code. Please try again."
                );
            }
        }
    }
}