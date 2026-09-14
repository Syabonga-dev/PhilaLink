namespace PersonalProject.Models.DTOs
{
    public class RegisterClinicAdminDto
    {
        public string FullName { get; set; } = string.Empty;

        public string IdNumber { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        /*
         * ClinicAdmin must always belong to a clinic.
         */
        public Guid ClinicId { get; set; }
    }
}