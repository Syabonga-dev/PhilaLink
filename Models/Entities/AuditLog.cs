namespace PersonalProject.Models.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }

        public string Action { get; set; } = string.Empty;

        public Guid? PerformedByUserId { get; set; }

        public User? PerformedByUser { get; set; }

        /*
         * Null = system-wide action.
         *
         * Non-null = action associated with a specific clinic.
         */
        public Guid? ClinicId { get; set; }

        public Clinic? Clinic { get; set; }

        public string Details { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } =
            DateTime.UtcNow;
    }
}