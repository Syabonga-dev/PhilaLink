namespace PersonalProject.Services.Interfaces
{
    public interface IOtpVerificationService
    {
        Task<DateTime> GenerateAsync(
            Guid userId
        );

        Task<bool> VerifyAsync(
            Guid userId,
            string code
        );
    }
}