namespace PersonalProject.Models.DTOs
{
    public class ChatbotMessageRequestDto
    {
        public string Message { get; set; } =
            string.Empty;
    }

    public class ChatbotMessageResponseDto
    {
        public Guid ConversationId { get; set; }

        public string Message { get; set; } =
            string.Empty;

        public DateTime CreatedAt { get; set; }
    }

    public class ChatbotHistoryMessageDto
    {
        public Guid Id { get; set; }

        public string Role { get; set; } =
            string.Empty;

        public string Content { get; set; } =
            string.Empty;

        public DateTime CreatedAt { get; set; }
    }

    public class ChatbotHistoryDto
    {
        public Guid ConversationId { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public List<ChatbotHistoryMessageDto> Messages { get; set; } =
            new();
    }
}