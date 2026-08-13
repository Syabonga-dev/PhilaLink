namespace PersonalProject.Models.Entities
{
    public class Medication
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public User? Patient { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public ICollection<MedicationSchedule> Schedules { get; set; } = new List<MedicationSchedule>();
        public ICollection<MedicationLog> Logs { get; set; } = new List<MedicationLog>();
    }
}

