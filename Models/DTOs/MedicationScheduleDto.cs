namespace PersonalProject.Models.DTOs
{
    public class MedicationScheduleDto
    {
        public Guid MedicationId { get; set; }

        public string TimeOfDay { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}