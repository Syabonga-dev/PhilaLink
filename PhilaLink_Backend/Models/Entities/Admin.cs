using PhilaLink_Backend.Models.Entities;

namespace PhilaLink_Backend.Models
{
    public class Admin
    {
        public int Id { get; set; }


        // Link to authentication User
        public Guid UserId { get; set; }


        // Admin profile information
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // Navigation
        public User User { get; set; } = null!;
    }
}