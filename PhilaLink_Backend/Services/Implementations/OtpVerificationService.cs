using Microsoft.EntityFrameworkCore;
using PhilaLink_Backend.Data;
using PhilaLink_Backend.Models.Entities;
using PhilaLink_Backend.Services.Interfaces;

namespace PhilaLink_Backend.Services.Implementations
{
    public class OtpVerificationService : IOtpVerificationService
    {
        private readonly PhilaLinkDbContext _context;

        public OtpVerificationService(PhilaLinkDbContext context)
        {
            _context = context;
        }

        public async Task<OtpVerification> GenerateAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found");

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

            return otp;
        }

        public async Task<bool> VerifyAsync(Guid userId, string code)
        {
            var otp = await _context.OtpVerifications
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Code == code);

            if (otp == null || otp.IsUsed || otp.ExpiryTime < DateTime.UtcNow)
                return false;

            otp.IsUsed = true;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}