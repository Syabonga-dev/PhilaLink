namespace PersonalProject.Models.DTOs
{
    public class MedicationResponseDto
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public string Dosage { get; set; } =
            string.Empty;

        public string Form { get; set; } =
            string.Empty;

        public string Instructions { get; set; } =
            string.Empty;

        public decimal? UnitsPerDose { get; set; }

        public string? PrescribedBy { get; set; }

        public string? ConditionName { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; }

        public List<string> ScheduleTimes { get; set; } =
            new();

        public DateTime? NextDoseAt { get; set; }
    }
}