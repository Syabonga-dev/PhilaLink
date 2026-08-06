namespace PhilaLink_Backend.Models.Entities
{
    public class MedicalCondition
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public string ConditionName { get; set; } = string.Empty;

        public DateOnly? DiagnosisDate { get; set; }

        public bool IsChronic { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
