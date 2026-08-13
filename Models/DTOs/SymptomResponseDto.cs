namespace PersonalProject.Models.DTOs
{
    public class SymptomResponseDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string Symptoms { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}


