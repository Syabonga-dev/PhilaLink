namespace PersonalProject.Models.Entities
{
    public class Nurse
    {
        public Guid Id { get; set; }

        // =====================================================
        // USER
        // =====================================================

        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        // =====================================================
        // PROFESSIONAL INFORMATION
        // =====================================================

        public string EmployeeNumber { get; set; } = string.Empty;

        public string RegistrationNumber { get; set; } = string.Empty;

        public string Qualification { get; set; } = string.Empty;

        // =====================================================
        // CLINIC
        // =====================================================

        public Guid ClinicId { get; set; }

        public Clinic Clinic { get; set; } = null!;

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
        // PERSONAL INFORMATION
        // =====================================================

        public DateOnly DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;

        // =====================================================
        // EMPLOYMENT
        // =====================================================

        public DateTime EmploymentDate { get; set; }

        // =====================================================
        // EMERGENCY CONTACT
        // =====================================================

        public string EmergencyContactName { get; set; } = string.Empty;

        public string EmergencyContactPhone { get; set; } = string.Empty;

        public string EmergencyContactRelationship { get; set; } = string.Empty;

        // =====================================================
        // AUDIT
        // =====================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}