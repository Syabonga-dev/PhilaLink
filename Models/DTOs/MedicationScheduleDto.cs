namespace PhilaLink_Backend.Models.DTOs
{
    public class MedicationScheduleDto
    {
        public int MedicationId { get; set; }
        public string TimeOfDay { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
