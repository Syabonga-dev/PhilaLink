namespace PersonalProject.Models.DTOs
{
    public class MedicationLogDto
    {
        public int MedicationId { get; set; }
        public DateTime TakenAt { get; set; }
        public bool WasTaken { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}


