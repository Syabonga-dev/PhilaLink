namespace PersonalProject.Models.Entities
{
    public class ChatMessage
    {
        public Guid Id { get; set; }

        public Guid ConversationId { get; set; }

        public string Role { get; set; } =
            string.Empty;

        public string Content { get; set; } =
            string.Empty;

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;

        public ChatConversation Conversation { get; set; } =
            null!;
    }
}