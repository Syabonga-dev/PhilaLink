namespace PersonalProject.Services.Interfaces
{
    public interface INotificationService
    {
        Task<bool> CreateForPatientAsync(
            Guid patientUserId,
            string message,
            Guid performedByUserId
        );

        Task<bool> CreateSystemForPatientAsync(
            Guid patientUserId,
            string message
        );
    }
}
