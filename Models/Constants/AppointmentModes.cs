namespace PersonalProject.Models.Constants
{
    public static class AppointmentModes
    {
        public const string InPerson = "InPerson";

        public const string Telehealth = "Telehealth";

        public static readonly string[] All =
        {
            InPerson,
            Telehealth
        };

        public static string Normalize(
            string? mode
        )
        {
            if (
                string.Equals(
                    mode,
                    Telehealth,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return Telehealth;
            }

            return InPerson;
        }
    }
}