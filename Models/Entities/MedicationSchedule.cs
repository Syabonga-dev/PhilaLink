namespace PersonalProject.Models.Entities
{
    public class MedicationSchedule
    {
        public Guid Id { get; set; }

        public Guid MedicationId { get; set; }
        public  Medication? Medication { get; set; }

        public TimeSpan TimeOfDay { get; set; }
        public bool IsActive { get; set; } = true;
    }
}


