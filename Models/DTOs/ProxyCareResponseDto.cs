namespace PersonalProject.Models.DTOs
{
    public class ProxyCareResponseDto
    {
        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } =
            string.Empty;

        public int TotalPatients { get; set; }

        public int DueSoon { get; set; }

        public int Overdue { get; set; }

        public List<ProxyCarePatientDto> Patients
        {
            get;
            set;
        } = new();
    }

    public class ProxyCarePatientDto
    {
        public Guid ProxyLinkId { get; set; }

        public Guid PatientId { get; set; }

        public string PatientName { get; set; } =
            string.Empty;

        public string PatientNumber { get; set; } =
            string.Empty;

        public Guid? ClinicId { get; set; }

        public string? ClinicName { get; set; }

        public DateTime AssignedAt { get; set; }

        public Guid? NextCollectionId { get; set; }

        public DateTime? NextCollectionDate { get; set; }

        public string CollectionStatus { get; set; } =
            "None";
    }

    public class ProxyCollectionResponseDto
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public string PatientName { get; set; } =
            string.Empty;

        public string PatientNumber { get; set; } =
            string.Empty;

        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } =
            string.Empty;

        public Guid? ProxyId { get; set; }

        public string? ProxyName { get; set; }

        public Guid? ProcessedByNurseId { get; set; }

        public string? ProcessedByNurseName { get; set; }

        public DateTime ScheduledCollectionDate
        {
            get;
            set;
        }

        public DateTime? CollectedAt { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string MedicationName { get; set; } =
            string.Empty;

        public string? Notes { get; set; }

        public List<ProxyCollectionItemDto> Items
        {
            get;
            set;
        } = new();
    }

    public class ProxyCollectionItemDto
    {
        public Guid Id { get; set; }

        public Guid MedicationId { get; set; }

        public string MedicationName { get; set; } =
            string.Empty;

        public string Dosage { get; set; } =
            string.Empty;

        public string Form { get; set; } =
            string.Empty;

        public int Quantity { get; set; }
    }
}