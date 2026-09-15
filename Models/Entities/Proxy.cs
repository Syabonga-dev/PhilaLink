namespace PersonalProject.Models.Entities
{
    public class Proxy
    {
        public Guid Id { get; set; }

        // =====================================================
        // USER
        // =====================================================

        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

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

        public string RelationshipToPatient { get; set; } = string.Empty;

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

        // =====================================================
        // RELATIONSHIPS
        // =====================================================

        public ICollection<ProxyLink> ProxyLinksAsProxy { get; set; }
            = new List<ProxyLink>();
    }
}