namespace PersonalProject.Models.Entities
{
    public class Proxy
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        public string Email { get; set; } = string.Empty;

        public string AddressLine1 { get; set; } = string.Empty;

        public string? AddressLine2 { get; set; }

        public string Suburb { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Province { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public DateOnly DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string RelationshipToPatient { get; set; } = string.Empty;

        public string EmergencyContactName { get; set; } = string.Empty;

        public string EmergencyContactPhone { get; set; } = string.Empty;

        public string EmergencyContactRelationship { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<ProxyLink> ProxyLinksAsProxy { get; set; }
            = new List<ProxyLink>();
    }
}