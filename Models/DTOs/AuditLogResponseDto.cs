namespace PersonalProject.Models.DTOs
{
    public class AuditLogResponseDto
    {
        public Guid Id { get; set; }

        public string Action { get; set; } = string.Empty;

        public string PerformedBy { get; set; } = string.Empty;

        public string Details { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }
    }
}