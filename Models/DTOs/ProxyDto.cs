namespace PersonalProject.Models.DTOs
{
    public class AssignProxyDto
    {
        public Guid PatientId { get; set; }

        public Guid ProxyId { get; set; }
    }

    public class ProxyPatientResponseDto
    {
        public Guid ProxyLinkId { get; set; }

        public Guid PatientId { get; set; }

        public string PatientName { get; set; } =
            string.Empty;

        public string PatientNumber { get; set; } =
            string.Empty;

        public Guid? ClinicId { get; set; }

        public string? ClinicName { get; set; }

        public DateTime AssignedAt { get; set; }
    }

    public class PatientProxyResponseDto
    {
        public Guid ProxyLinkId { get; set; }

        public Guid ProxyId { get; set; }

        public string ProxyName { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public DateTime AssignedAt { get; set; }
    }
}