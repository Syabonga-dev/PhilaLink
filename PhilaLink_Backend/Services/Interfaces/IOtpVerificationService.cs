using PhilaLink_Backend.Models.Entities;

namespace PhilaLink_Backend.Services.Interfaces
{
    public interface IOtpVerificationService
    {
        Task<OtpVerification> GenerateAsync(Guid userId);
        Task<bool> VerifyAsync(Guid userId, string code);
    }
}