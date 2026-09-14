namespace PersonalProject.Models.Entities
{
    public class HealthMetric
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public Patient Patient { get; set; } = null!;

        /*
         * BloodPressure
         * BloodGlucose
         * Weight
         * etc.
         */
        public string MetricType { get; set; } =
            string.Empty;

        public string Value { get; set; } =
            string.Empty;

        public string Unit { get; set; } =
            string.Empty;

        public string? Status { get; set; }

        public string? Note { get; set; }

        public DateTime RecordedAt { get; set; } =
            DateTime.UtcNow;
    }
}