namespace PersonalProject.Models.DTOs
{
    public class ProxyLinkCreateDto
    {
        public Guid PatientId { get; set; }

        public Guid ProxyId { get; set; }
    }
}