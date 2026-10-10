using System.ComponentModel.DataAnnotations;

namespace PersonalProject.Models.DTOs
{
    public class RegisterClinicAdminDto
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

        /*
         * ClinicAdmin must always belong to a clinic.
         */
        public Guid ClinicId { get; set; }
    }
}