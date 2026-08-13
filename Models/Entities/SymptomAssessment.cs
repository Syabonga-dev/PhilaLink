namespace PersonalProject.Models.Entities
{
    public class SymptomAssessment
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }
        public User Patient { get; set; } = null!;

        public string SymptomsJson { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}


