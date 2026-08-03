namespace PhilaLink_Backend.Models.DTOs
{
    public class NotificationDto
    {
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }
}
