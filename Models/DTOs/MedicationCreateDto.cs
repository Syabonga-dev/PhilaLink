namespace PersonalProject.Models.DTOs
{
    public class MedicationCreateDto
    {
        public int PatientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
    }
}


