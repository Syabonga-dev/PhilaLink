namespace PersonalProject.Models.Entities
{
    public class Appointment
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public Patient Patient { get; set; } = null!;

        public Guid ClinicId { get; set; }

        public Clinic Clinic { get; set; } = null!;

        public Guid? NurseId { get; set; }

        public Nurse? Nurse { get; set; }

        public DateTime ScheduledAt { get; set; }

        public int DurationMinutes { get; set; } = 30;

        public string Type { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string? ProviderName { get; set; }

        /*
         * InPerson
         * Telehealth
         */
        public string Mode { get; set; } = "InPerson";

        /*
         * Scheduled
         * Confirmed
         * Pending
         * Completed
         * Cancelled
         * Missed
         * Rescheduled
         */
        public string Status { get; set; } = "Scheduled";

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}