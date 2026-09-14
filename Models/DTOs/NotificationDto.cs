namespace PersonalProject.Models.DTOs
{
    public class NotificationDto
    {
        public Guid UserId { get; set; }

        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }
    }
}