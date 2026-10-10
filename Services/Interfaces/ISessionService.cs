namespace PersonalProject.Services.Interfaces
{
    public interface ISessionService
    {
        Task RevokeAllSessionsAsync(
            Guid userId
        );
    }
}