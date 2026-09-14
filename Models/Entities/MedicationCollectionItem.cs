namespace PersonalProject.Models.Entities
{
    public class MedicationCollectionItem
    {
        public Guid Id { get; set; }

        // Collection
        public Guid MedicationCollectionId { get; set; }

        public MedicationCollection MedicationCollection
        {
            get;
            set;
        } = null!;

        // Patient medication/prescription
        public Guid MedicationId { get; set; }

        public Medication Medication { get; set; } = null!;

        // Exact clinic inventory item being issued
        public Guid ClinicStockId { get; set; }

        public ClinicStock ClinicStock { get; set; } = null!;

        public int Quantity { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}