using PersonalProject.Models.Validation;

namespace PersonalProject.Models.DTOs
{
    public class CreateAppointmentDto
    {
        public Guid PatientId { get; set; }

        public Guid ClinicId { get; set; }

        public Guid? NurseId { get; set; }

        /*
         * Staff-created appointments follow the same booking
         * rule as patient-created appointments:
         *
         * - today is allowed
         * - the selected time must still be in the future
         * - appointments can be created up to 14 days ahead
         */
        [AppointmentBookingWindow]
        public DateTime ScheduledAt { get; set; }

        public int DurationMinutes { get; set; } = 30;

        public string Type { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string? ProviderName { get; set; }

        public string Mode { get; set; } = "InPerson";

        public string? Notes { get; set; }
    }

    public class UpdateAppointmentDto
    {
        /*
         * This DTO is used for several appointment actions,
         * including:
         *
         * - Pending -> Confirmed
         * - rescheduling
         * - completion
         * - cancellation
         * - missed appointments
         *
         * Active appointments must remain inside the 14-day
         * booking window.
         *
         * Historical Completed, Cancelled and Missed records
         * are excluded from the date-window check so nurses can
         * still update their status/details after the scheduled
         * appointment time.
         */
        [
            AppointmentBookingWindow(
                SkipTerminalStatuses = true
            )
        ]
        public DateTime ScheduledAt { get; set; }

        public Guid? NurseId { get; set; }

        public int DurationMinutes { get; set; } = 30;

        public string Type { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string? ProviderName { get; set; }

        public string Mode { get; set; } = "InPerson";

        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }

    public class AppointmentResponseDto
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } = string.Empty;

        public Guid? NurseId { get; set; }

        public string? NurseName { get; set; }

        public DateTime ScheduledAt { get; set; }

        public int DurationMinutes { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string? ProviderName { get; set; }

        public string Mode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }
}