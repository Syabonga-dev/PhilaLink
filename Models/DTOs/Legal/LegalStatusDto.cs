namespace PersonalProject.Models.DTOs.Legal
{
    public class LegalStatusDto
    {
        /*
         * True when the user has at least one current
         * legal document that still requires acceptance
         * or acknowledgement.
         */
        public bool RequiresAction { get; set; }

        /*
         * Contains all currently applicable legal
         * documents together with the user's acceptance
         * status for each one.
         */
        public List<LegalDocumentDto> Documents { get; set; } =
            new List<LegalDocumentDto>();
    }
}