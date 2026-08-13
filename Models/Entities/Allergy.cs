namespace PersonalProject.Models.Entities
{
    public class Allergy
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public string AllergyName { get; set; } = string.Empty;

        public string? Reaction { get; set; }

        public string? Severity { get; set; }

        public string? Notes { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}


