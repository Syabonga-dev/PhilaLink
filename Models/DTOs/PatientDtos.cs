namespace PersonalProject.Models.DTOs
{
    public class PatientMeDto
    {
        public Guid PatientId { get; set; }

        public Guid UserId { get; set; }

        public string PatientNumber { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string IdNumber { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateOnly? DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string AddressLine1 { get; set; } = string.Empty;

        public string? AddressLine2 { get; set; }

        public string Suburb { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Province { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string EmergencyContactName { get; set; } =
            string.Empty;

        public string EmergencyContactPhone { get; set; } =
            string.Empty;

        public string EmergencyContactRelationship { get; set; } =
            string.Empty;

        public Guid? ClinicId { get; set; }

        public string? ClinicName { get; set; }

        public bool IsProfileComplete { get; set; }

        public List<PatientAllergyDto> Allergies { get; set; } =
            new();

        public List<PatientConditionDto> Conditions { get; set; } =
            new();
    }

    public class UpdatePatientProfileDto
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public DateOnly DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string AddressLine1 { get; set; } = string.Empty;

        public string? AddressLine2 { get; set; }

        public string Suburb { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Province { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string EmergencyContactName { get; set; } =
            string.Empty;

        public string EmergencyContactPhone { get; set; } =
            string.Empty;

        public string EmergencyContactRelationship { get; set; } =
            string.Empty;
    }

    public class PatientAllergyDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Reaction { get; set; }

        public string? Severity { get; set; }

        public string? Notes { get; set; }
    }

    public class PatientConditionDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateOnly? DiagnosisDate { get; set; }

        public bool IsChronic { get; set; }

        public string? Notes { get; set; }
    }

    public class PatientPreferenceDto
    {
        public bool MedicationReminders { get; set; }

        public bool AppointmentReminders { get; set; }

        public bool ClinicNotifications { get; set; }

        public bool HealthUpdates { get; set; }

        public bool ShareHealthData { get; set; }

        public bool AllowChatbotProfileAccess { get; set; }
    }

    public class PatientDashboardDto
    {
        public string FullName { get; set; } = string.Empty;

        public string PatientNumber { get; set; } = string.Empty;

        public bool IsProfileComplete { get; set; }

        public PatientClinicSummaryDto? Clinic { get; set; }

        public List<PatientMedicationDto> Medications { get; set; } =
            new();

        public List<AppointmentResponseDto> UpcomingAppointments
        {
            get;
            set;
        } = new();

        public List<HealthMetricResponseDto> HealthMetrics
        {
            get;
            set;
        } = new();

        public int UnreadNotifications { get; set; }

        public MedicationCollectionResponseDto? NextCollection
        {
            get;
            set;
        }
    }

    public class PatientClinicSummaryDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string ContactNumber { get; set; } = string.Empty;

        public TimeSpan? OpeningTime { get; set; }

        public TimeSpan? ClosingTime { get; set; }
    }

    public class PatientMedicationDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Dosage { get; set; } = string.Empty;

        public string Form { get; set; } = string.Empty;

        public string Instructions { get; set; } = string.Empty;

        public string? PrescribedBy { get; set; }

        public string? ConditionName { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; }

        public List<string> ScheduleTimes { get; set; } = new();

        public DateTime? NextDoseAt { get; set; }

        public int? DaysRemaining { get; set; }
    }

    public class HealthMetricResponseDto
    {
        public Guid Id { get; set; }

        public string MetricType { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public string? Status { get; set; }

        public string? Note { get; set; }

        public DateTime RecordedAt { get; set; }
    }

    public class HealthRecordResponseDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string? ProviderName { get; set; }

        public string? Facility { get; set; }

        public string Summary { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime RecordDate { get; set; }
    }

    public class PatientBookAppointmentDto
    {
        public DateTime ScheduledAt { get; set; }

        public int DurationMinutes { get; set; } = 30;

        public string Type { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string Mode { get; set; } = "InPerson";

        public string? Notes { get; set; }
    }

    public class PatientRescheduleAppointmentDto
    {
        public DateTime ScheduledAt { get; set; }
    }
}