namespace PersonalProject.Models.Entities
{
    public class User
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
        // Admin, Nurse, Patient, Proxy

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // Domain relationships

        public Patient? Patient { get; set; }

        public Nurse? Nurse { get; set; }

        public Proxy? Proxy { get; set; }

        public Admin? Admin { get; set; }


        // Proxy relationships

        public ICollection<ProxyLink> ProxyLinksAsPatient { get; set; }
            = new List<ProxyLink>();

        public ICollection<ProxyLink> ProxyLinksAsProxy { get; set; }
            = new List<ProxyLink>();
    }
}

