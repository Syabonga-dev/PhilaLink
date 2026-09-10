namespace PersonalProject.Models.DTOs
{
    // What we hand back to the frontend after login. Deliberately excludes
    // PasswordHash — never serialize that out over the API.
    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}