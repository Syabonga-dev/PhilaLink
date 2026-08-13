namespace PersonalProject.Models.Entities
{
    public class Patient
    {
        public Guid Id { get; set; }

        // Authentication
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        // Personal Information
        public DateOnly DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;

        // Contact
        public string Email { get; set; } = string.Empty;

        // Address
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string Suburb { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;

        // Emergency Contact
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string EmergencyContactRelationship { get; set; } = string.Empty;

        // Status
        public bool IsActive { get; set; } = true;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Relationships
        public ICollection<Allergy> Allergies { get; set; } = new List<Allergy>();
        public ICollection<MedicalCondition> MedicalConditions { get; set; } = new List<MedicalCondition>();
    }
}


