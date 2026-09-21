using System.ComponentModel.DataAnnotations;

namespace PersonalProject.Models.DTOs.Legal
{
    public class AcceptLegalDocumentRequest
    {
        [Required]
        public Guid LegalDocumentId { get; set; }

        /*
         * Expected values:
         *
         * Accepted
         * Acknowledged
         *
         * The backend service will still validate this
         * value rather than trusting the frontend.
         */
        [Required]
        [MaxLength(30)]
        public string Action { get; set; } =
            string.Empty;
    }
}