using System.ComponentModel.DataAnnotations;

namespace PersonalProject.Models.DTOs
{
    public class LoginDto
    {
        [Required]
        [StringLength(32, MinimumLength = 1)]
        public string IdNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 1)]
        public string Password { get; set; } = string.Empty;
    }
}