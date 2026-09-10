namespace PersonalProject.Models.DTOs
{
    public class AdminAccountDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    // Returned once, right after registering a Nurse/Proxy, so the Admin can
    // relay the login credentials. The plaintext temp password only ever
    // appears in this one response — it's not stored anywhere or
    // retrievable afterward (only the bcrypt hash is kept).
    public class NewStaffAccountDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public string TemporaryPassword { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}