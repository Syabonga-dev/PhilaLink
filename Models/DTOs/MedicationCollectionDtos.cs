namespace PersonalProject.Models.DTOs
{
    public class CreateMedicationCollectionDto
    {
        public Guid PatientId { get; set; }

        public Guid ClinicId { get; set; }

        public DateTime ScheduledCollectionDate
        {
            get;
            set;
        }

        public string? Notes { get; set; }

        public List<CreateMedicationCollectionItemDto>
            Items
        { get; set; } = new();
    }

    public class CreateMedicationCollectionItemDto
    {
        public Guid MedicationId { get; set; }

        public int Quantity { get; set; }

        public string? Notes { get; set; }
    }

    public class AssignCollectionProxyDto
    {
        public Guid ProxyId { get; set; }
    }

    public class CompleteMedicationCollectionDto
    {
        /*
         * Null means patient collected personally.
         */
        public Guid? ProxyId { get; set; }

        public string? Notes { get; set; }
    }

    public class MedicationCollectionItemResponseDto
    {
        public Guid Id { get; set; }

        public Guid MedicationId { get; set; }

        public string MedicationName { get; set; } =
            string.Empty;

        public string Dosage { get; set; } =
            string.Empty;

        public int Quantity { get; set; }

        public string? Notes { get; set; }
    }

    public class MedicationCollectionResponseDto
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public string PatientName { get; set; } =
            string.Empty;

        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } =
            string.Empty;

        public Guid? ProxyId { get; set; }

        public string? ProxyName { get; set; }

        public DateTime ScheduledCollectionDate
        {
            get;
            set;
        }

        public DateTime? CollectedAt { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string? Notes { get; set; }

        public List<MedicationCollectionItemResponseDto>
            Items
        { get; set; } = new();
    }
}