namespace PersonalProject.Models.DTOs
{
    public class SuperAdminAccountDto
    {
        public Guid UserId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string IdNumber { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public string Role { get; set; } =
            string.Empty;

        public Guid? ClinicId { get; set; }

        public string? ClinicName { get; set; }

        public bool IsActive { get; set; }

        public bool IsVerified { get; set; }

        public bool MustChangePassword { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
