namespace PersonalProject.Models.DTOs
{
    public class CreateAppointmentDto
    {
        public Guid PatientId { get; set; }

        public Guid ClinicId { get; set; }

        public Guid? NurseId { get; set; }

        public DateTime ScheduledAt { get; set; }

        public string Type { get; set; } =
            string.Empty;

        public string Reason { get; set; } =
            string.Empty;

        public string? Notes { get; set; }
    }

    public class UpdateAppointmentDto
    {
        public DateTime ScheduledAt { get; set; }

        public Guid? NurseId { get; set; }

        public string Type { get; set; } =
            string.Empty;

        public string Reason { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public string? Notes { get; set; }
    }

    public class AppointmentResponseDto
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public string PatientName { get; set; } =
            string.Empty;

        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } =
            string.Empty;

        public Guid? NurseId { get; set; }

        public string? NurseName { get; set; }

        public DateTime ScheduledAt { get; set; }

        public string Type { get; set; } =
            string.Empty;

        public string Reason { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public string? Notes { get; set; }
    }
}