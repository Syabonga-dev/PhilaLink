namespace PersonalProject.Models.DTOs
{
    public class MedicationCreateDto
    {
        public Guid PatientId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Dosage { get; set; } = string.Empty;

        public string Form { get; set; } = string.Empty;

        public string Instructions { get; set; } = string.Empty;

        public string? PrescribedBy { get; set; }

        public string? ConditionName { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}