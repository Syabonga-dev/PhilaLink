using System.ComponentModel.DataAnnotations;

namespace PersonalProject.Models.DTOs
{
    public class ChangePasswordDto
    {
        [Required]
        [StringLength(128, MinimumLength = 1)]
        public string CurrentPassword { get; set; } =
            string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 12)]
        public string NewPassword { get; set; } =
            string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 12)]
        public string ConfirmNewPassword { get; set; } =
            string.Empty;
    }
}