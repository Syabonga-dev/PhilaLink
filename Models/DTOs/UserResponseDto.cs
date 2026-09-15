namespace PersonalProject.Models.DTOs
{
    public class UserResponseDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string IdNumber { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string Role { get; set; } =
            string.Empty;

        public bool IsActive { get; set; }

        public bool IsVerified { get; set; }

        public bool MustChangePassword { get; set; }
    }
}