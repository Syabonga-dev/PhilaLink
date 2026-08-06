namespace PhilaLink_Backend.Models.DTOs
{
    public class ProxyLinkResponseDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int ProxyId { get; set; }
        public int AssignedByNurseId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
