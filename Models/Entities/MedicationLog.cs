namespace PhilaLink_Backend.Models.Entities
{
    public class MedicationLog
    {
        public Guid Id { get; set; }

        public Guid MedicationId { get; set; }
        public Medication? Medication { get; set; }

        public DateTime TakenAt { get; set; } = DateTime.UtcNow;

        public bool Taken { get; set; }
    }
}