namespace PersonalProject.Models.DTOs
{
    // =====================================================
    // PROXY ASSIGNMENT
    // =====================================================

    public class AssignProxyDto
    {
        public Guid PatientId { get; set; }

        public Guid ProxyId { get; set; }
    }

    // =====================================================
    // PROXY: LINKED PATIENT
    // =====================================================

    public class ProxyPatientResponseDto
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
    }

    // =====================================================
    // PATIENT: ASSIGNED PROXY
    // =====================================================

    public class PatientProxyResponseDto
    {
        public Guid ProxyLinkId { get; set; }

        public Guid ProxyId { get; set; }

        public string ProxyName { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public DateTime AssignedAt { get; set; }
    }

    public class PatientAssignedWorkerDto
    {
        public Guid ProxyLinkId { get; set; }

        public Guid ProxyId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public DateTime AssignedAt { get; set; }

        public bool IsActive { get; set; }
    }

    // =====================================================
    // PROXY PROFILE
    // =====================================================

    public class ProxyMeDto
    {
        // -------------------------------------------------
        // IDENTIFIERS
        // -------------------------------------------------

        public Guid ProxyId { get; set; }

        public Guid UserId { get; set; }

        // -------------------------------------------------
        // ACCOUNT
        // -------------------------------------------------

        public string FullName { get; set; } =
            string.Empty;

        public string IdNumber { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        // -------------------------------------------------
        // PERSONAL
        // -------------------------------------------------

        public DateOnly DateOfBirth { get; set; }

        public string Gender { get; set; } =
            string.Empty;

        public string RelationshipToPatient { get; set; } =
            string.Empty;

        // -------------------------------------------------
        // ADDRESS
        // -------------------------------------------------

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

        // -------------------------------------------------
        // EMERGENCY CONTACT
        // -------------------------------------------------

        public string EmergencyContactName { get; set; } =
            string.Empty;

        public string EmergencyContactPhone { get; set; } =
            string.Empty;

        public string EmergencyContactRelationship { get; set; } =
            string.Empty;

        // -------------------------------------------------
        // CLINIC
        // -------------------------------------------------

        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } =
            string.Empty;

        // -------------------------------------------------
        // ACCOUNT STATE
        // -------------------------------------------------

        public bool IsActive { get; set; }

        public bool IsVerified { get; set; }

        public bool MustChangePassword { get; set; }

        // -------------------------------------------------
        // AUDIT
        // -------------------------------------------------

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    // =====================================================
    // PROXY PROFILE UPDATE
    // =====================================================

    /*
     * Important:
     *
     * The Proxy is intentionally NOT allowed to modify:
     *
     * - ProxyId
     * - UserId
     * - IdNumber
     * - ClinicId
     * - Role
     * - IsActive
     * - IsVerified
     *
     * ClinicId in particular is an authorization boundary.
     * Clinic reassignment remains an administrative action.
     */
    public class UpdateProxyProfileDto
    {
        // -------------------------------------------------
        // ACCOUNT
        // -------------------------------------------------

        public string FullName { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        // -------------------------------------------------
        // PERSONAL
        // -------------------------------------------------

        public DateOnly DateOfBirth { get; set; }

        public string Gender { get; set; } =
            string.Empty;

        public string RelationshipToPatient { get; set; } =
            string.Empty;

        // -------------------------------------------------
        // ADDRESS
        // -------------------------------------------------

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

        // -------------------------------------------------
        // EMERGENCY CONTACT
        // -------------------------------------------------

        public string EmergencyContactName { get; set; } =
            string.Empty;

        public string EmergencyContactPhone { get; set; } =
            string.Empty;

        public string EmergencyContactRelationship { get; set; } =
            string.Empty;
    }
}