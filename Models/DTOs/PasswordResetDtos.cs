using System.ComponentModel.DataAnnotations;

namespace PersonalProject.Models.DTOs
{
    public class ForgotPasswordRequestDto
    {
        [Required]
        [StringLength(254, MinimumLength = 1)]
        public string Identifier
        {
            get;
            set;
        } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required]
        [StringLength(254, MinimumLength = 1)]
        public string Identifier
        {
            get;
            set;
        } = string.Empty;

        [Required]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage =
                "Verification code must contain exactly 6 digits."
        )]
        public string Code
        {
            get;
            set;
        } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 12)]
        public string NewPassword
        {
            get;
            set;
        } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 12)]
        public string ConfirmNewPassword
        {
            get;
            set;
        } = string.Empty;
    }
}
