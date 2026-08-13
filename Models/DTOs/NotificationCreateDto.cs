namespace PersonalProject.Models.DTOs
{
    public class NotificationCreateDto
    {
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}


