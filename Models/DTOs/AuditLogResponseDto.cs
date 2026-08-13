namespace PersonalProject.Models.DTOs
{
    public class AuditLogResponseDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}


