namespace PersonalProject.Models.DTOs
{
    public class MedicationCreateDto
    {
        public Guid PatientId { get; set; }

        // Nurse must select medication from clinic inventory.
        public Guid ClinicStockId { get; set; }

        /*
         * Kept for compatibility with existing clients.
         * The backend now takes Name, Dosage/Strength and Form
         * from ClinicStock instead of trusting free text.
         */
        public string Name { get; set; } =
            string.Empty;

        public string Dosage { get; set; } =
            string.Empty;

        public string Form { get; set; } =
            string.Empty;

        public string Instructions { get; set; } =
            string.Empty;

        public decimal? UnitsPerDose { get; set; }

        public string? PrescribedBy { get; set; }

        public string? ConditionName { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
