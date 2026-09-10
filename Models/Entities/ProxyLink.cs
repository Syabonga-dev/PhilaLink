using PersonalProject.Models;

namespace PersonalProject.Models.Entities
{
    public class ProxyLink
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public Guid ProxyId { get; set; }

        public Guid? AssignedByNurseId { get; set; }

        public int? AssignedByAdminId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Patient Patient { get; set; } = null!;

        public Proxy Proxy { get; set; } = null!;

        public Nurse? AssignedByNurse { get; set; }

        public Admin? AssignedByAdmin { get; set; }
    }
}