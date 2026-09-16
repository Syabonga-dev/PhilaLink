namespace PersonalProject.Models.DTOs
{
    public class MedicationSupplyDto
    {
        public Guid MedicationId { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public string Dosage { get; set; } =
            string.Empty;

        public string Form { get; set; } =
            string.Empty;

        public decimal? UnitsPerDose { get; set; }

        public int DosesPerDay { get; set; }

        public int? DispensedQuantity { get; set; }

        public decimal? EstimatedRemainingQuantity
        {
            get;
            set;
        }

        public int? DaysRemaining { get; set; }

        public DateTime? LastCollectedAt { get; set; }

        public string CalculationStatus { get; set; } =
            string.Empty;
    }
}