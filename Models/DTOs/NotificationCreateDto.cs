namespace PersonalProject.Models.DTOs
{
    public class NotificationCreateDto
    {
        public Guid UserId { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}