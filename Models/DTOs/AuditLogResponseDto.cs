namespace PersonalProject.Models.DTOs
{
    public class AuditLogResponseDto
    {
        public Guid Id { get; set; }

        public string Action { get; set; } =
            string.Empty;

        public Guid? PerformedByUserId { get; set; }

        public string PerformedBy { get; set; } =
            string.Empty;

        public string PerformedByRole { get; set; } =
            string.Empty;

        public Guid? ClinicId { get; set; }

        public string? ClinicName { get; set; }

        public string Details { get; set; } =
            string.Empty;

        public DateTime Timestamp { get; set; }
    }
}
