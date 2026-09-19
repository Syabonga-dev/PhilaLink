using System.ComponentModel.DataAnnotations;

namespace PersonalProject.Models.DTOs
{
    public class ForgotPasswordRequestDto
    {
        [Required]
        public string Identifier
        {
            get;
            set;
        } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required]
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
        [MinLength(
            12,
            ErrorMessage =
                "Password must contain at least 12 characters."
        )]
        public string NewPassword
        {
            get;
            set;
        } = string.Empty;

        [Required]
        public string ConfirmNewPassword
        {
            get;
            set;
        } = string.Empty;
    }
}
