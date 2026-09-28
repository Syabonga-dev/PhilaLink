namespace PersonalProject.Models.DTOs
{
    public class UpdateDateOfBirthDto
    {
        public DateOnly DateOfBirth { get; set; }
    }

    public class UpdateIdNumberDto
    {
        public string IdNumber { get; set; } =
            string.Empty;
    }

    public class IdentityResponseDto
    {
        public string IdNumber { get; set; } =
            string.Empty;

        public DateOnly DateOfBirth { get; set; }
    }
}
