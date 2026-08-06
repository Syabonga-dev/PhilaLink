namespace PhilaLink_Backend.Models.DTOs
{
    public class VerifyOtpDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

    }
}
