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

        public DateTime AssignedAt { get; set; } =
            DateTime.UtcNow;

        public bool IsActive { get; set; } =
            true;

        public DateTime? EndedAt { get; set; }

        public Guid? EndedByUserId { get; set; }

        public string? EndReason { get; set; }

        // Navigation
        public Patient Patient { get; set; } =
            null!;

        public Proxy Proxy { get; set; } =
            null!;

        public Nurse? AssignedByNurse { get; set; }

        public Admin? AssignedByAdmin { get; set; }

        public User? EndedByUser { get; set; }
    }
}