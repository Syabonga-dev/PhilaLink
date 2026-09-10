using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using System.Net;
using System.Net.Mail;

namespace PersonalProject.Services.Implementations
{
    public class OtpVerificationService : IOtpVerificationService
    {
        private readonly PhilaLinkDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<OtpVerificationService> _logger;

        public OtpVerificationService(
            PhilaLinkDbContext context,
            IConfiguration config,
            ILogger<OtpVerificationService> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        public async Task<OtpVerification> GenerateAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new InvalidOperationException(
                    "This account has no email address on file to send a code to.");

            var generatedCode = new Random().Next(100000, 999999).ToString();

            var otp = new OtpVerification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Code = generatedCode,
                ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            };

            _context.OtpVerifications.Add(otp);
            await _context.SaveChangesAsync();

            await SendOtpEmailAsync(user.Email, user.FullName, generatedCode);

            return otp;
        }

        public async Task<bool> VerifyAsync(Guid userId, string code)
        {
            // Most-recent-first: if the user hit "resend", earlier codes for
            // the same user still exist in the table, so match the latest.
            var otp = await _context.OtpVerifications
                .Where(x => x.UserId == userId && x.Code == code)
                .OrderByDescending(x => x.ExpiryTime)
                .FirstOrDefaultAsync();

            if (otp == null || otp.IsUsed || otp.ExpiryTime < DateTime.UtcNow)
                return false;

            otp.IsUsed = true;
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task SendOtpEmailAsync(string toEmail, string recipientName, string code)
        {
            var host = _config["Smtp:Host"];

            if (string.IsNullOrWhiteSpace(host))
            {
                // Dev fallback: no SMTP configured yet. Log the code instead
                // of crashing, so you can keep testing other functionality
                // before SMTP credentials are set up. Remove this fallback
                // once real SMTP config is in place.
                _logger.LogWarning(
                    "Smtp:Host not configured — not sending email. OTP for {Email} is {Code}.",
                    toEmail, code);
                return;
            }

            var port = int.Parse(_config["Smtp:Port"] ?? "587");
            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];
            var fromAddress = _config["Smtp:From"] ?? username ?? "no-reply@philalink.dev";
            var enableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "true");

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, "PhilaLink"),
                Subject = "Your PhilaLink verification code",
                Body = $"Hi {recipientName},\n\nYour PhilaLink verification code is: {code}\n\n" +
                       "This code expires in 5 minutes.",
                IsBodyHtml = false
            };
            message.To.Add(toEmail);

            try
            {
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
                throw new Exception("Failed to send verification email. Please try again.");
            }
        }
    }
}