namespace PersonalProject.Models.DTOs.Legal
{
    public class LegalDocumentDto
    {
        public Guid Id { get; set; }

        public string Type { get; set; } =
            string.Empty;

        public string Title { get; set; } =
            string.Empty;

        public string Version { get; set; } =
            string.Empty;

        public DateTime EffectiveDate { get; set; }

        public bool IsCurrent { get; set; }

        /*
         * Indicates whether the currently authenticated
         * user has already completed the required action
         * for this specific document version.
         */
        public bool HasAccepted { get; set; }

        /*
         * Examples:
         *
         * Accepted
         * Acknowledged
         *
         * Null when the user has not yet completed
         * the required action.
         */
        public string? Action { get; set; }

        /*
         * UTC timestamp of the user's acceptance or
         * acknowledgement.
         *
         * Null when no acceptance exists yet.
         */
        public DateTime? AcceptedAt { get; set; }
    }
}