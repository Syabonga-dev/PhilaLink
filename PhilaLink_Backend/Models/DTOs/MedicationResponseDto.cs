namespace PhilaLink_Backend.Models.DTOs
{
    public class MedicationResponseDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
    }
}
