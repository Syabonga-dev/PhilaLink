namespace PersonalProject.Models.DTOs
{
    public class ProxyLinkResponseDto
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public Guid ProxyId { get; set; }

        public Guid? AssignedByNurseId { get; set; }

        public int? AssignedByAdminId { get; set; }

        public DateTime AssignedAt { get; set; }
    }
}