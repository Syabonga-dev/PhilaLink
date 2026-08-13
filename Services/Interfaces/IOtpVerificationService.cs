using PersonalProject.Models.Entities;

namespace PersonalProject.Services.Interfaces
{
    public interface IOtpVerificationService
    {
        Task<OtpVerification> GenerateAsync(Guid userId);
        Task<bool> VerifyAsync(Guid userId, string code);
    }
}

