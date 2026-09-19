using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<AppointmentResponseDto>
            CreateAsync(
                CreateAppointmentDto dto,
                Guid performedByUserId
            );

        Task<AppointmentResponseDto>
            UpdateAsync(
                Guid appointmentId,
                UpdateAppointmentDto dto,
                Guid performedByUserId
            );

        Task DeleteAsync(
            Guid appointmentId,
            Guid performedByUserId
        );

        Task<AppointmentResponseDto?>
            GetByIdAsync(
                Guid appointmentId,
                Guid performedByUserId
            );

        Task<List<AppointmentResponseDto>>
            GetClinicAppointmentsAsync(
                Guid performedByUserId
            );

        Task<List<AppointmentResponseDto>>
            GetPatientAppointmentsAsync(
                Guid patientUserId
            );
    }
}
