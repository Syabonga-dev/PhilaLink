namespace PersonalProject.Models.Entities
{
    public class ClinicStock
    {
        public Guid Id { get; set; }

        // =====================================================
        // CLINIC
        // =====================================================

        public Guid ClinicId { get; set; }

        public Clinic Clinic { get; set; } = null!;

        // =====================================================
        // MEDICATION DETAILS
        // =====================================================

        public string MedicationName { get; set; } =
            string.Empty;

        public string Strength { get; set; } =
            string.Empty;

        public string Form { get; set; } =
            string.Empty;

        /*
         * Examples:
         *
         * tablets
         * capsules
         * bottles
         * units
         */
        public string Unit { get; set; } =
            string.Empty;

        // =====================================================
        // INVENTORY
        // =====================================================

        public int QuantityOnHand { get; set; }

        public int ReorderLevel { get; set; }

        public bool IsActive { get; set; } = true;

        // =====================================================
        // AUDIT
        // =====================================================

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}