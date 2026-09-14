namespace PersonalProject.Models.DTOs
{
    public class MedicationLogDto
    {
        public Guid MedicationId { get; set; }

        public DateTime TakenAt { get; set; }

        public bool WasTaken { get; set; }

        public string? Notes { get; set; }
    }
}