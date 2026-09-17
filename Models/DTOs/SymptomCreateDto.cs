namespace PersonalProject.Models.DTOs
{
    public class SymptomCreateDto
    {
        public string Symptoms { get; set; } = string.Empty;

        public int? Age { get; set; }

        public string? Duration { get; set; }

        public List<string> Allergies { get; set; } =
            new();

        public List<string> Medications { get; set; } =
            new();

        public List<string> Conditions { get; set; } =
            new();
    }
}
