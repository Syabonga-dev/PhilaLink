namespace PersonalProject.Models.Entities
{
    public class MedicationCollection
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public Guid ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        public Guid? ProxyId { get; set; }
        public Proxy? Proxy { get; set; }

        public Guid? ProcessedByNurseId { get; set; }
        public Nurse? ProcessedByNurse { get; set; }

        public DateTime ScheduledCollectionDate { get; set; }

        public DateTime? CollectedAt { get; set; }

        public string Status { get; set; } = "Scheduled";

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<MedicationCollectionItem> Items { get; set; }
            = new List<MedicationCollectionItem>();
    }
}