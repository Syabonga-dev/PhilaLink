namespace PersonalProject.Models.DTOs
{
    public class SuperAdminMeDto
    {
        public Guid UserId { get; set; }

        public int AdminId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public bool IsActive { get; set; }
    }

    public class AssignClinicAdminDto
    {
        public Guid ClinicId { get; set; }
    }

    public class SuperAdminClinicAdminDto
    {
        public Guid UserId { get; set; }

        public int AdminId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public bool IsActive { get; set; }

        public Guid? ClinicId { get; set; }

        public string? ClinicName { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    public class SuperAdminAnalyticsQueryDto
    {
        public Guid? ClinicId { get; set; }

        public DateTime? DateFrom { get; set; }

        public DateTime? DateTo { get; set; }
    }

    public class SuperAdminAnalyticsDto
    {
        public Guid? ClinicId { get; set; }

        public string ClinicName { get; set; } =
            "All clinics";

        public DateTime DateFrom { get; set; }

        public DateTime DateTo { get; set; }

        public int TotalClinics { get; set; }

        public int ActiveClinics { get; set; }

        public int TotalPatients { get; set; }

        public int ActivePatients { get; set; }

        public int TotalNurses { get; set; }

        public int ActiveNurses { get; set; }

        public int TotalProxies { get; set; }

        public int ActiveProxies { get; set; }

        public int Appointments { get; set; }

        public int Collections { get; set; }

        public int MissedCollections { get; set; }

        public int MedicationLogs { get; set; }

        public int MissedMedicationLogs { get; set; }

        public double MedicationAdherenceRate { get; set; }

        public int LowStockItems { get; set; }

        public List<SuperAdminClinicMetricDto>
            Clinics
        { get; set; } =
            new();

        public List<SuperAdminTrendPointDto>
            Trend
        { get; set; } =
            new();
    }

    public class SuperAdminClinicMetricDto
    {
        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } =
            string.Empty;

        public bool IsActive { get; set; }

        public int Patients { get; set; }

        public int Nurses { get; set; }

        public int Proxies { get; set; }

        public int ClinicAdmins { get; set; }

        public int Appointments { get; set; }

        public int Collections { get; set; }

        public int MissedCollections { get; set; }

        public int LowStockItems { get; set; }
    }

    public class SuperAdminTrendPointDto
    {
        public string Period { get; set; } =
            string.Empty;

        public int Registrations { get; set; }

        public int Appointments { get; set; }

        public int Collections { get; set; }
    }
}
