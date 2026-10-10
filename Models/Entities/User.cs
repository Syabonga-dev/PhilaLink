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

        public int FailedLoginAttempts { get; set; }

        public DateTime? LastFailedLoginAtUtc { get; set; }

        public DateTime? LockoutEndUtc { get; set; }

        public void ClearLoginAbuseState()
        {
            FailedLoginAttempts = 0;
            LastFailedLoginAtUtc = null;
            LockoutEndUtc = null;
        }

        /*
         * Security version embedded into every PhilaLink JWT.
         *
         * Incrementing this value invalidates every JWT that
         * was issued with an older TokenVersion.
         */
        public int TokenVersion { get; set; } = 1;

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

        // =====================================================
        // LEGAL ACCEPTANCES
        // =====================================================

        /*
         * Historical legal acceptance records are retained
         * so PhilaLink can determine which version of each
         * document this user accepted or acknowledged.
         */
        public ICollection<UserLegalAcceptance>
            LegalAcceptances
        { get; set; } =
                new List<UserLegalAcceptance>();
    }
}