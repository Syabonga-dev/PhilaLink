namespace PersonalProject.Models.DTOs
{
    public class RegisterResponseDto
    {
        public Guid UserId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string Role { get; set; } =
            string.Empty;

        public bool RequiresVerification { get; set; }
    }
}