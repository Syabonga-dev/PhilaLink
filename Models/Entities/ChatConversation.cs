namespace PersonalProject.Models.Entities
{
    public class ChatConversation
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public DateTime StartedAt { get; set; } =
            DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } =
            DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public Patient Patient { get; set; } = null!;

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}