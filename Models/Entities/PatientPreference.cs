namespace PersonalProject.Models.Entities
{
    public class PatientPreference
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public Patient Patient { get; set; } = null!;

        public bool MedicationReminders { get; set; } =
            true;

        public bool AppointmentReminders { get; set; } =
            true;

        public bool ClinicNotifications { get; set; } =
            true;

        public bool HealthUpdates { get; set; }

        public bool ShareHealthData { get; set; } =
            true;

        public bool AllowChatbotProfileAccess { get; set; } =
            true;

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}