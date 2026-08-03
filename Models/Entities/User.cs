namespace PhilaLink_Backend.Models.Entities
{
    public class User
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty; // Patient, Nurse, Proxy

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation (IMPORTANT FIX)
        public ICollection<ProxyLink> ProxyLinksAsPatient { get; set; } = new List<ProxyLink>();
        public ICollection<ProxyLink> ProxyLinksAsProxy { get; set; } = new List<ProxyLink>();
    }
}
