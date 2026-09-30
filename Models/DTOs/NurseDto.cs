namespace PersonalProject.Models.DTOs
{
    public class NurseMeDto
    {
        public Guid NurseId { get; set; }

        public Guid UserId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string IdNumber { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string EmployeeNumber { get; set; } =
            string.Empty;

        public string RegistrationNumber { get; set; } =
            string.Empty;

        public string Qualification { get; set; } =
            string.Empty;

        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } =
            string.Empty;

        public string AddressLine1 { get; set; } =
            string.Empty;

        public string? AddressLine2 { get; set; }

        public string Suburb { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public string Province { get; set; } =
            string.Empty;

        public string PostalCode { get; set; } =
            string.Empty;

        public DateOnly DateOfBirth { get; set; }

        public string Gender { get; set; } =
            string.Empty;

        public DateTime EmploymentDate { get; set; }

        public string EmergencyContactName { get; set; } =
            string.Empty;

        public string EmergencyContactPhone { get; set; } =
            string.Empty;

        public string EmergencyContactRelationship { get; set; } =
            string.Empty;

        public bool IsActive { get; set; }

        public bool IsVerified { get; set; }

        public bool MustChangePassword { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateNurseProfileDto
    {
        public string FullName { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string Gender { get; set; } =
            string.Empty;

        public string AddressLine1 { get; set; } =
            string.Empty;

        public string? AddressLine2 { get; set; }

        public string Suburb { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public string Province { get; set; } =
            string.Empty;

        public string PostalCode { get; set; } =
            string.Empty;

        public string EmergencyContactName { get; set; } =
            string.Empty;

        public string EmergencyContactPhone { get; set; } =
            string.Empty;

        public string EmergencyContactRelationship { get; set; } =
            string.Empty;
    }

    public class NurseDashboardDto
    {
        public int ClinicPatients { get; set; }

        public int AppointmentsToday { get; set; }

        public int PendingAppointments { get; set; }

        public int CollectionsDueToday { get; set; }

        public int OverdueCollections { get; set; }
    }

    public class NursePatientDto
    {
        public Guid PatientId { get; set; }

        public Guid UserId { get; set; }

        public string PatientNumber { get; set; } =
            string.Empty;

        public string FullName { get; set; } =
            string.Empty;

        public DateOnly? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string PhoneNumber { get; set; } =
            string.Empty;

        public int AllergyCount { get; set; }

        public int ActiveMedicationCount { get; set; }

        public int OverdueCollectionCount { get; set; }

        public DateTime? NextCollectionDate { get; set; }

        public DateTime? NextAppointmentAt { get; set; }
    }

    public class NurseAlertDto
    {
        public string Code { get; set; } =
            string.Empty;

        public string Severity { get; set; } =
            string.Empty;

        public string Message { get; set; } =
            string.Empty;

        public int Count { get; set; }
    }

    public class NursePatientCareDto
    {
        public Guid PatientId { get; set; }

        public Guid UserId { get; set; }

        public string PatientNumber { get; set; } =
            string.Empty;

        public string FullName { get; set; } =
            string.Empty;

        public string IdNumber { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public DateOnly? DateOfBirth { get; set; }

        public string Gender { get; set; } =
            string.Empty;

        public string AddressLine1 { get; set; } =
            string.Empty;

        public string? AddressLine2 { get; set; }

        public string Suburb { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public string Province { get; set; } =
            string.Empty;

        public string PostalCode { get; set; } =
            string.Empty;

        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } =
            string.Empty;

        public List<PatientAllergyDto> Allergies { get; set; } =
            new();

        public List<NurseConditionDto> Conditions { get; set; } =
            new();

        public List<HealthMetricResponseDto> HealthMetrics { get; set; } =
            new();

        public List<NurseMedicationDto> Medications { get; set; } =
            new();

        public List<NurseCollectionDto> Collections { get; set; } =
            new();

        public List<NurseAppointmentDto> Appointments { get; set; } =
            new();

        public List<NursePatientProxyDto> Proxies { get; set; } =
            new();
    }

    public class NurseConditionDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public DateOnly? DiagnosisDate { get; set; }

        public bool IsChronic { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; }
    }

    public class NurseMedicationDto
    {
        public Guid Id { get; set; }

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

        public bool IsActive { get; set; }

        public List<NurseMedicationScheduleDto> Schedules
        {
            get;
            set;
        } = new();
    }

    public class NurseMedicationScheduleDto
    {
        public Guid Id { get; set; }

        public string TimeOfDay { get; set; } =
            string.Empty;

        public bool IsActive { get; set; }
    }

    public class NurseCollectionDto
    {
        public Guid Id { get; set; }

        public DateTime ScheduledCollectionDate { get; set; }

        public DateTime? CollectedAt { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string MedicationName { get; set; } =
            string.Empty;

        public int Quantity { get; set; }

        public Guid? ProxyId { get; set; }

        public string? ProxyName { get; set; }

        public Guid? ProcessedByNurseId { get; set; }

        public string? ProcessedByNurseName { get; set; }

        public string? Notes { get; set; }
    }

    public class NurseAppointmentDto
    {
        public Guid Id { get; set; }

        public DateTime ScheduledAt { get; set; }

        public int DurationMinutes { get; set; }

        public string Type { get; set; } =
            string.Empty;

        public string Reason { get; set; } =
            string.Empty;

        public string? ProviderName { get; set; }

        public string Mode { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public string? Notes { get; set; }

        public Guid? NurseId { get; set; }

        public string? NurseName { get; set; }
    }

    public class NursePatientProxyDto
    {
        public Guid ProxyLinkId { get; set; }

        public Guid ProxyId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public DateTime AssignedAt { get; set; }
    }

    public class NurseClinicProxyDto
    {
        public Guid ProxyId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;
    }

    public class NurseAllergyWriteDto
    {
        public string Name { get; set; } =
            string.Empty;

        public string? Reaction { get; set; }

        public string? Severity { get; set; }

        public string? Notes { get; set; }
    }

    public class NurseConditionWriteDto
    {
        public string Name { get; set; } =
            string.Empty;

        public DateOnly? DiagnosisDate { get; set; }

        public bool IsChronic { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; } =
            true;
    }

    public class NurseHealthMetricWriteDto
    {
        public string MetricType { get; set; } =
            string.Empty;

        public string Value { get; set; } =
            string.Empty;

        public string Unit { get; set; } =
            string.Empty;

        public string? Status { get; set; }

        public string? Note { get; set; }
    }

    public class NurseScheduleCollectionDto
    {
        public Guid MedicationId { get; set; }

        public DateTime ScheduledCollectionDate { get; set; }

        public int Quantity { get; set; } =
            1;

        public Guid? ProxyId { get; set; }

        public string? Notes { get; set; }
    }
}
