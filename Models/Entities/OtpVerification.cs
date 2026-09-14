namespace PersonalProject.Models.Entities
{
    public class OtpVerification
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        /*
         * Never store the verification code itself.
         * Only the BCrypt hash is persisted.
         */
        public string CodeHash { get; set; } =
            string.Empty;

        public DateTime ExpiryTime { get; set; }

        public bool IsUsed { get; set; } = false;

        public int AttemptCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;
    }
}