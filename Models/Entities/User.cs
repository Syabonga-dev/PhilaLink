namespace PersonalProject.Models.Entities
{
    public class User
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

        public string PasswordHash { get; set; } =
            string.Empty;

        public string Role { get; set; } =
            string.Empty;

        // =====================================================
        // ACCOUNT STATE
        // =====================================================

        public bool IsActive { get; set; } = true;

        public bool IsVerified { get; set; } = true;

        public DateTime? VerifiedAt { get; set; }

        /*
         * Administrator-created accounts receive a temporary
         * password and must replace it before normal API use.
         */
        public bool MustChangePassword { get; set; }

        // =====================================================
        // AUDIT
        // =====================================================

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // =====================================================
        // PROFILES
        // =====================================================

        public Patient? Patient { get; set; }

        public Nurse? Nurse { get; set; }

        public Proxy? Proxy { get; set; }

        public Admin? Admin { get; set; }
    }
}