namespace PersonalProject.Models.DTOs
{
    public class ClinicResponseDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string ContactNumber { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string Services { get; set; } = string.Empty;

        public TimeSpan? OpeningTime { get; set; }

        public TimeSpan? ClosingTime { get; set; }

        public bool IsActive { get; set; }
    }
}