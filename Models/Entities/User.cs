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

        /*
         * Administrative account state.
         *
         * False means the account has been deactivated.
         *
         * Do not use this property for OTP/email verification.
         */
        public bool IsActive { get; set; } = true;

        /*
         * Identity/contact verification state.
         *
         * Staff accounts created by administrators are considered
         * verified unless explicitly changed during creation.
         *
         * Patient self-registration overrides this to false.
         */
        public bool IsVerified { get; set; } = true;

        public DateTime? VerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public Patient? Patient { get; set; }

        public Nurse? Nurse { get; set; }

        public Proxy? Proxy { get; set; }

        public Admin? Admin { get; set; }
    }
}