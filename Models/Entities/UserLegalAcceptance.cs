namespace PersonalProject.Models.Entities
{
    public class UserLegalAcceptance
    {
        public Guid Id { get; set; }

        // =====================================================
        // USER
        // =====================================================

        public Guid UserId { get; set; }

        public User User { get; set; } =
            null!;

        // =====================================================
        // LEGAL DOCUMENT
        // =====================================================

        public Guid LegalDocumentId { get; set; }

        public LegalDocument LegalDocument { get; set; } =
            null!;

        // =====================================================
        // ACCEPTANCE
        // =====================================================

        /*
         * Expected values:
         *
         * Accepted
         * Acknowledged
         *
         * Terms of Use should normally use:
         * Accepted
         *
         * Privacy Policy should normally use:
         * Acknowledged
         */
        public string Action { get; set; } =
            string.Empty;

        /*
         * UTC timestamp showing when the user
         * completed the legal action.
         */
        public DateTime AcceptedAt { get; set; } =
            DateTime.UtcNow;
    }
}