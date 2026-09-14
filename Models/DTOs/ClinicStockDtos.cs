namespace PersonalProject.Models.DTOs
{
    public class CreateClinicStockDto
    {
        public Guid ClinicId { get; set; }

        public string MedicationName { get; set; } =
            string.Empty;

        public string Strength { get; set; } =
            string.Empty;

        public string Form { get; set; } =
            string.Empty;

        public string Unit { get; set; } =
            string.Empty;

        public int QuantityOnHand { get; set; }

        public int ReorderLevel { get; set; }
    }

    public class UpdateClinicStockDto
    {
        public int QuantityOnHand { get; set; }

        public int ReorderLevel { get; set; }

        public bool IsActive { get; set; }
    }

    public class AdjustClinicStockDto
    {
        /*
         * Positive = stock added
         * Negative = stock removed
         */
        public int QuantityChange { get; set; }

        public string? Reason { get; set; }
    }

    public class ClinicStockResponseDto
    {
        public Guid Id { get; set; }

        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } =
            string.Empty;

        public string MedicationName { get; set; } =
            string.Empty;

        public string Strength { get; set; } =
            string.Empty;

        public string Form { get; set; } =
            string.Empty;

        public string Unit { get; set; } =
            string.Empty;

        public int QuantityOnHand { get; set; }

        public int ReorderLevel { get; set; }

        public bool IsLowStock =>
            QuantityOnHand <= ReorderLevel;

        public bool IsActive { get; set; }
    }
}