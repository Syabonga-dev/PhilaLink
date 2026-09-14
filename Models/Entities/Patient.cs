namespace PersonalProject.Models.Entities
{
    public class Patient
    {
        public Guid Id { get; set; }

        // =====================================================
        // USER ACCOUNT
        // =====================================================

        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        public string PatientNumber { get; set; } = string.Empty;
        public bool IsProfileComplete { get; set; }

        public DateTime? ProfileCompletedAt { get; set; }

        // =====================================================
        // CLINIC
        // =====================================================

        /*
         * Nullable during registration/onboarding.
         *
         * A patient may create an account before a clinic
         * has officially been assigned to them.
         *
         * Once linked by clinic staff, this identifies their
         * primary clinic.
         */
        public Guid? ClinicId { get; set; }

        public Clinic? Clinic { get; set; }

        // =====================================================
        // PERSONAL INFORMATION
        // =====================================================

        public DateOnly DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;

        // =====================================================
        // CONTACT
        // =====================================================

        public string Email { get; set; } = string.Empty;

        // =====================================================
        // ADDRESS
        // =====================================================

        public string AddressLine1 { get; set; } = string.Empty;

        public string? AddressLine2 { get; set; }

        public string Suburb { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Province { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        // =====================================================
        // EMERGENCY CONTACT
        // =====================================================

        public string EmergencyContactName { get; set; } = string.Empty;

        public string EmergencyContactPhone { get; set; } = string.Empty;

        public string EmergencyContactRelationship { get; set; } = string.Empty;

        // =====================================================
        // LEGACY PROFILE STATUS
        // =====================================================

        /*
         * Kept temporarily because existing services currently
         * reference Patient.IsActive.
         *
         * User.IsActive becomes the authoritative account-level
         * status as the architecture migration continues.
         */
        public bool IsActive { get; set; } = true;

        // =====================================================
        // AUDIT
        // =====================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // =====================================================
        // RELATIONSHIPS
        // =====================================================

        public ICollection<Allergy> Allergies { get; set; }
            = new List<Allergy>();

        public ICollection<MedicalCondition> MedicalConditions { get; set; }
            = new List<MedicalCondition>();

        public ICollection<ProxyLink> ProxyLinksAsPatient { get; set; }
            = new List<ProxyLink>();

        public ICollection<HealthMetric> HealthMetrics { get; set; }
    = new List<HealthMetric>();

        public ICollection<HealthRecord> HealthRecords { get; set; }
            = new List<HealthRecord>();

        public PatientPreference? Preference { get; set; }
    }
}