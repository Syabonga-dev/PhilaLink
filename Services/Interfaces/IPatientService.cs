using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IPatientService
    {
        Task<PatientMeDto> GetMeAsync(
            Guid userId
        );

        Task<PatientMeDto> UpdateMeAsync(
            Guid userId,
            UpdatePatientProfileDto dto
        );

        Task<PatientDashboardDto> GetDashboardAsync(
            Guid userId
        );

        Task<List<PatientMedicationDto>>
            GetMedicationsAsync(
                Guid userId
            );

        Task<List<AppointmentResponseDto>>
            GetAppointmentsAsync(
                Guid userId
            );

        Task<AppointmentResponseDto>
            BookAppointmentAsync(
                Guid userId,
                PatientBookAppointmentDto dto
            );

        Task<AppointmentResponseDto>
            RescheduleAppointmentAsync(
                Guid userId,
                Guid appointmentId,
                PatientRescheduleAppointmentDto dto
            );

        Task CancelAppointmentAsync(
            Guid userId,
            Guid appointmentId
        );

        Task<List<HealthRecordResponseDto>>
            GetRecordsAsync(
                Guid userId
            );

        Task<List<MedicationCollectionResponseDto>>
            GetCollectionsAsync(
                Guid userId
            );

        Task<MedicationCollectionResponseDto?>
            GetNextCollectionAsync(
                Guid userId
            );

        Task<List<NotificationResponseDto>>
            GetNotificationsAsync(
                Guid userId
            );

        Task MarkNotificationReadAsync(
            Guid userId,
            Guid notificationId
        );

        Task<PatientPreferenceDto>
            GetPreferencesAsync(
                Guid userId
            );

        Task<PatientPreferenceDto>
            UpdatePreferencesAsync(
                Guid userId,
                PatientPreferenceDto dto
            );
    }
}