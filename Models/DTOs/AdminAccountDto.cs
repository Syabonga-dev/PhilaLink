namespace PersonalProject.Models.DTOs
{
    public class AdminAccountDto
    {
        public Guid UserId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string IdNumber { get; set; } =
            string.Empty;

        public string Role { get; set; } =
            string.Empty;

        public bool IsActive { get; set; }
    }

    /*
     * Account creation responses deliberately do NOT contain
     * the generated temporary password.
     *
     * The plaintext password exists only inside the backend
     * long enough to send the onboarding email.
     */
    public class NewStaffAccountDto
    {
        public Guid UserId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string IdNumber { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string Role { get; set; } =
            string.Empty;

        public Guid? ClinicId { get; set; }

        public string? ClinicName { get; set; }

        public bool EmailSent { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}
