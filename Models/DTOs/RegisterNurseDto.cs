using System.ComponentModel.DataAnnotations;

namespace PersonalProject.Models.DTOs
{
    public class RegisterNurseDto
    {
        [StringLength(120)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(32)]
        public string IdNumber { get; set; } = string.Empty;

        [StringLength(32)]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [StringLength(32)]
        public string EmployeeNumber { get; set; } = string.Empty;

        [StringLength(32)]
        public string RegistrationNumber { get; set; } = string.Empty;

        [StringLength(120)]
        public string Qualification { get; set; } = string.Empty;
        public Guid ClinicId { get; set; }

        [StringLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [StringLength(100)]
        public string Suburb { get; set; } = string.Empty;

        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(100)]
        public string Province { get; set; } = string.Empty;

        [StringLength(16)]
        public string PostalCode { get; set; } = string.Empty;

        public DateOnly DateOfBirth { get; set; }
        [StringLength(32)]
        public string Gender { get; set; } = string.Empty;
        public DateTime EmploymentDate { get; set; }

        [StringLength(120)]
        public string EmergencyContactName { get; set; } = string.Empty;

        [StringLength(32)]
        public string EmergencyContactPhone { get; set; } = string.Empty;

        [StringLength(80)]
        public string EmergencyContactRelationship { get; set; } = string.Empty;
    }
}