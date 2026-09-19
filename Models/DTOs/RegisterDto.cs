using System.ComponentModel.DataAnnotations;

namespace PersonalProject.Models.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string FullName
        {
            get;
            set;
        } = string.Empty;

        [Required]
        public string IdNumber
        {
            get;
            set;
        } = string.Empty;

        [Required]
        public string PhoneNumber
        {
            get;
            set;
        } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email
        {
            get;
            set;
        } = string.Empty;

        [Required]
        [MinLength(12)]
        public string Password
        {
            get;
            set;
        } = string.Empty;
    }
}
