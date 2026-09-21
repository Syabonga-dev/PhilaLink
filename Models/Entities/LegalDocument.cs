namespace PersonalProject.Models.Entities
{
    public class LegalDocument
    {
        public Guid Id { get; set; }

        // Examples:
        // TermsOfUse
        // PrivacyPolicy
        public string Type { get; set; } =
            string.Empty;

        public string Title { get; set; } =
            string.Empty;

        /*
         * Version must change whenever the legal
         * wording changes in a way that requires
         * users to accept or acknowledge it again.
         *
         * Examples:
         * 1.0
         * 1.1
         * 2.0
         */
        public string Version { get; set; } =
            string.Empty;

        /*
         * Date from which this document version
         * becomes applicable.
         */
        public DateTime EffectiveDate { get; set; }

        /*
         * Only the current version of each document
         * type should normally have IsCurrent = true.
         */
        public bool IsCurrent { get; set; }

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;

        /*
         * Acceptance records are intentionally kept.
         *
         * When a new document version is published,
         * we do not delete the previous acceptances.
         * This provides an audit history showing
         * which version each user accepted.
         */
        public ICollection<UserLegalAcceptance>
            UserAcceptances
        { get; set; } =
                new List<UserLegalAcceptance>();
    }
}