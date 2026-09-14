namespace PersonalProject.Services.Interfaces
{
    public interface IOtpVerificationService
    {
        Task<DateTime> GenerateAsync(
            Guid userId,
            string purpose
        );

        Task<bool> VerifyAsync(
            Guid userId,
            string code,
            string purpose
        );
    }
}