namespace PersonalProject.Models.DTOs
{
    public class CreateClinicDto
    {
        public string Name { get; set; } =
            string.Empty;

        public string Type { get; set; } =
            "Clinic";

        public string Address { get; set; } =
            string.Empty;

        public string ContactNumber { get; set; } =
            string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string Services { get; set; } =
            string.Empty;

        public TimeSpan? OpeningTime { get; set; }

        public TimeSpan? ClosingTime { get; set; }
    }

    public class UpdateClinicDto
    {
        public string Name { get; set; } =
            string.Empty;

        public string Type { get; set; } =
            string.Empty;

        public string Address { get; set; } =
            string.Empty;

        public string ContactNumber { get; set; } =
            string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string Services { get; set; } =
            string.Empty;

        public TimeSpan? OpeningTime { get; set; }

        public TimeSpan? ClosingTime { get; set; }

        public bool IsActive { get; set; }
    }
}