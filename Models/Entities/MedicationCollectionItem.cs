namespace PersonalProject.Models.Entities
{
    public class MedicationCollectionItem
    {
        public Guid Id { get; set; }

        // =====================================================
        // COLLECTION
        // =====================================================

        public Guid MedicationCollectionId { get; set; }

        public MedicationCollection MedicationCollection
        {
            get;
            set;
        } = null!;

        // =====================================================
        // PATIENT MEDICATION
        // =====================================================

        public Guid MedicationId { get; set; }

        public Medication Medication { get; set; } = null!;

        // =====================================================
        // QUANTITY
        // =====================================================

        public int Quantity { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;
    }
}