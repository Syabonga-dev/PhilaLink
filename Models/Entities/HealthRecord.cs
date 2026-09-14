namespace PersonalProject.Models.Entities
{
    public class HealthRecord
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public Patient Patient { get; set; } = null!;

        public Guid? ClinicId { get; set; }

        public Clinic? Clinic { get; set; }

        public string Title { get; set; } =
            string.Empty;

        /*
         * Consultation
         * Laboratory
         * Medication
         * Observation
         */
        public string Type { get; set; } =
            string.Empty;

        public string Category { get; set; } =
            string.Empty;

        public string? ProviderName { get; set; }

        public string Summary { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            "Available";

        public DateTime RecordDate { get; set; }

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}