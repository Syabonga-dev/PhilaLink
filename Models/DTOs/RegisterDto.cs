using System.ComponentModel.DataAnnotations;

namespace PersonalProject.Models.DTOs
{
    public class RegisterDto
    {
        [Required]
        [StringLength(120)]
        public string FullName
        {
            get;
            set;
        } = string.Empty;

        [Required]
        [StringLength(32, MinimumLength = 1)]
        public string IdNumber
        {
            get;
            set;
        } = string.Empty;

        [Required]
        [StringLength(32, MinimumLength = 1)]
        public string PhoneNumber
        {
            get;
            set;
        } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email
        {
            get;
            set;
        } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 12)]
        public string Password
        {
            get;
            set;
        } = string.Empty;
    }
}
