namespace PhilaLink_Backend.Models.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }

        public string Action { get; set; } = null!;

        public Guid? PerformedByUserId { get; set; }

        public User? PerformedByUser { get; set; }

        public string Details { get; set; } = null!;

        public DateTime Timestamp { get; set; }
    }
}