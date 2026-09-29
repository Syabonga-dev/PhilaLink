namespace PersonalProject.Models.DTOs
{
    public class ClinicAdminAnalyticsDto
    {
        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } =
            string.Empty;

        public string AdminName { get; set; } =
            string.Empty;

        public DateTime GeneratedAtUtc { get; set; }

        public ClinicAdminKpisDto Kpis { get; set; } =
            new();

        public List<ClinicAdminTrendPointDto>
            MonthlyActivity
        { get; set; } =
            new();

        public List<ClinicAdminStatusCountDto>
            AppointmentStatuses
        { get; set; } =
            new();

        public List<ClinicAdminStatusCountDto>
            CollectionStatuses
        { get; set; } =
            new();

        public List<ClinicAdminLowStockDto>
            LowStockItems
        { get; set; } =
            new();

        public List<ClinicAdminRecentActivityDto>
            RecentActivity
        { get; set; } =
            new();
    }

    public class ClinicAdminKpisDto
    {
        public int ActivePatients { get; set; }

        public int ActiveNurses { get; set; }

        public int ActiveProxies { get; set; }

        public int AppointmentsToday { get; set; }

        public int PendingAppointments { get; set; }

        public int CollectionsDueToday { get; set; }

        public int OverdueCollections { get; set; }

        public int LowStockItems { get; set; }

        public int CollectedThisMonth { get; set; }

        public int NewPatientsThisMonth { get; set; }
    }

    public class ClinicAdminTrendPointDto
    {
        public string Period { get; set; } =
            string.Empty;

        public int NewPatients { get; set; }

        public int Appointments { get; set; }

        public int CompletedAppointments { get; set; }

        public int Collections { get; set; }

        public int CompletedCollections { get; set; }
    }

    public class ClinicAdminStatusCountDto
    {
        public string Status { get; set; } =
            string.Empty;

        public int Count { get; set; }
    }

    public class ClinicAdminLowStockDto
    {
        public Guid Id { get; set; }

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

    public class ClinicAdminRecentActivityDto
    {
        public Guid Id { get; set; }

        public string Action { get; set; } =
            string.Empty;

        public string PerformedBy { get; set; } =
            string.Empty;

        public string Details { get; set; } =
            string.Empty;

        public DateTime Timestamp { get; set; }
    }

    public class ClinicAdminStaffDto
    {
        public Guid UserId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string IdNumber { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string Role { get; set; } =
            string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    // =========================================================
    // REPORT DATA
    // =========================================================

    public class ClinicAdminReportDataDto
    {
        public string ClinicName { get; set; } =
            string.Empty;

        public string AdminName { get; set; } =
            string.Empty;

        public DateTime GeneratedAtUtc { get; set; }

        public DateTime RangeStartUtc { get; set; }

        public DateTime RangeEndUtc { get; set; }

        public ClinicAdminAnalyticsDto Analytics
        { get; set; } =
            new();

        public List<ClinicAdminReportAppointmentDto>
            Appointments
        { get; set; } =
            new();

        public List<ClinicAdminReportCollectionDto>
            Collections
        { get; set; } =
            new();

        public List<ClinicAdminReportStockDto>
            Inventory
        { get; set; } =
            new();

        public List<ClinicAdminStaffDto>
            Staff
        { get; set; } =
            new();

        public List<ClinicAdminReportPatientDto>
            NewPatients
        { get; set; } =
            new();
    }

    public class ClinicAdminReportAppointmentDto
    {
        public DateTime ScheduledAt { get; set; }

        public string PatientName { get; set; } =
            string.Empty;

        public string Type { get; set; } =
            string.Empty;

        public string Mode { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public string NurseName { get; set; } =
            string.Empty;
    }

    public class ClinicAdminReportCollectionDto
    {
        public DateTime ScheduledCollectionDate
        { get; set; }

        public DateTime? CollectedAt { get; set; }

        public string PatientName { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public string ProxyName { get; set; } =
            string.Empty;

        public string ProcessedByNurseName
        { get; set; } =
            string.Empty;

        public int TotalQuantity { get; set; }
    }

    public class ClinicAdminReportStockDto
    {
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

        public bool IsActive { get; set; }

        public string Status { get; set; } =
            string.Empty;
    }

    public class ClinicAdminReportPatientDto
    {
        public string FullName { get; set; } =
            string.Empty;

        public string PatientNumber { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}