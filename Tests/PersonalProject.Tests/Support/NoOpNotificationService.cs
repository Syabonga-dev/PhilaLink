using PersonalProject.Services.Interfaces;

namespace PersonalProject.Tests.Support
{
    internal sealed class
        NoOpNotificationService :
        INotificationService
    {
        public static readonly
            NoOpNotificationService
            Instance =
                new();

        private NoOpNotificationService()
        {
        }

        public Task<bool>
            CreateForPatientAsync(
                Guid patientUserId,
                string message,
                Guid performedByUserId
            )
        {
            return Task.FromResult(
                true
            );
        }

        public Task<bool>
            CreateAppointmentForPatientAsync(
                Guid patientUserId,
                string message
            )
        {
            return Task.FromResult(
                true
            );
        }

        public Task<bool>
            CreateSystemForPatientAsync(
                Guid patientUserId,
                string message
            )
        {
            return Task.FromResult(
                true
            );
        }
    }
}
