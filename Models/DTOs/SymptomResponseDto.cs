namespace PersonalProject.Models.DTOs
{
    public class SymptomResponseDto
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public string Symptoms { get; set; } = string.Empty;

        public string Result { get; set; } = string.Empty;

        public string Recommendation { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}