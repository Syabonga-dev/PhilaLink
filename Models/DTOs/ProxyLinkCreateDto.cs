namespace PhilaLink_Backend.Models.DTOs
{
    public class ProxyLinkCreateDto
    {
        public int PatientId { get; set; }
        public int ProxyId { get; set; }
        public int AssignedByNurseId { get; set; }
    }
}
