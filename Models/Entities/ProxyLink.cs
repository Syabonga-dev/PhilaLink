namespace PersonalProject.Models.Entities
{
    public class ProxyLink
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }
        public Guid ProxyId { get; set; }
        public Guid AssignedByNurseId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Navigation (EF handles this)
        public User Patient { get; set; } = null!;
        public User Proxy { get; set; } = null!;
        public User AssignedByNurse { get; set; } = null!;
    }
}

