namespace PhilaLink_Backend.Models.DTOs
{
    public class SymptomCreateDto
    {
        public Guid PatientId { get; set; }
        public string Symptoms { get; set; } = string.Empty;
    }
}
